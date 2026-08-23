using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.OrgChart;

public sealed class GetOrgChartQueryHandler : IRequestHandler<GetOrgChartQuery, Result<OrgChartResult>>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;
    private readonly IReadRepository<Department> _departments;
    private readonly IReadRepository<Designation> _designations;
    private readonly ITenantContext _tenantContext;

    public GetOrgChartQueryHandler(
        IReadRepository<Employee> employees,
        IReadRepository<ReportingRelationship> reportingRelationships,
        IReadRepository<Department> departments,
        IReadRepository<Designation> designations,
        ITenantContext tenantContext)
    {
        _employees = employees;
        _reportingRelationships = reportingRelationships;
        _departments = departments;
        _designations = designations;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OrgChartResult>> Handle(GetOrgChartQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var employees = await _employees.ListAsync(new EmployeesByTenantSpecification(tenantId), cancellationToken);
        var relationships = await _reportingRelationships.ListAsync(
            new ActiveReportingRelationshipsSpecification(tenantId, request.AsOf), cancellationToken);
        var departments = await _departments.ListAsync(new DepartmentsByTenantSpecification(tenantId), cancellationToken);
        var designations = await _designations.ListAsync(new DesignationsByTenantSpecification(tenantId), cancellationToken);

        var employeesById = employees.ToDictionary(e => e.Id);
        var departmentNamesById = departments.ToDictionary(d => d.Id, d => d.Name);
        var designationTitlesById = designations.ToDictionary(d => d.Id, d => d.Title);

        var childIdsByManager = relationships
            .GroupBy(rr => rr.ManagerId)
            .ToDictionary(g => g.Key, g => g.Select(rr => rr.EmployeeId).ToList());
        var managedEmployeeIds = relationships.Select(rr => rr.EmployeeId).ToHashSet();

        List<EmployeeId> rootIds;
        if (request.RootEmployeeId is { } requestedRootId)
        {
            var rootEmployeeId = new EmployeeId(requestedRootId);
            if (!employeesById.ContainsKey(rootEmployeeId))
            {
                return Result.Failure<OrgChartResult>(Error.NotFound("employee.not_found", "Employee not found."));
            }

            rootIds = [rootEmployeeId];
        }
        else
        {
            // A root is anyone nobody is recorded as managing as of this date — the top of a
            // management chain, or a standalone employee with no reporting-relationship row at
            // all. There can be more than one: an org chart is a forest until every department
            // head shares one common manager.
            rootIds = [.. employeesById.Keys.Where(id => !managedEmployeeIds.Contains(id))];
        }

        var visited = new HashSet<EmployeeId>();
        var roots = rootIds
            .Select(id => BuildNode(id, request.AsOf, employeesById, departmentNamesById, designationTitlesById, childIdsByManager, visited))
            .Where(node => node is not null)
            .Select(node => node!)
            .OrderBy(node => node.FullName, StringComparer.Ordinal)
            .ToList();

        var etag = ComputeETag(tenantId, request.AsOf, request.RootEmployeeId, employees, relationships);

        return Result.Success(new OrgChartResult(etag, roots));
    }

    private static OrgChartNodeDto? BuildNode(
        EmployeeId employeeId,
        DateOnly asOf,
        IReadOnlyDictionary<EmployeeId, Employee> employeesById,
        IReadOnlyDictionary<DepartmentId, string> departmentNamesById,
        IReadOnlyDictionary<DesignationId, string> designationTitlesById,
        IReadOnlyDictionary<EmployeeId, List<EmployeeId>> childIdsByManager,
        HashSet<EmployeeId> visited)
    {
        // Defensive only: cycle detection at write time (CreateReportingRelationshipCommandHandler)
        // should make this unreachable, but a tree-builder walking pre-existing data should never
        // be able to recurse forever regardless.
        if (!visited.Add(employeeId) || !employeesById.TryGetValue(employeeId, out var employee))
        {
            return null;
        }

        var assignment = EmploymentHistoryTimeline.AsOf(employee.EmploymentHistory, asOf);
        var departmentId = assignment?.DepartmentId ?? employee.DepartmentId;
        var designationId = assignment?.DesignationId ?? employee.DesignationId;

        departmentNamesById.TryGetValue(departmentId, out var departmentName);
        designationTitlesById.TryGetValue(designationId, out var designationTitle);

        var children = childIdsByManager.TryGetValue(employeeId, out var childIds)
            ? childIds
                .Select(childId => BuildNode(childId, asOf, employeesById, departmentNamesById, designationTitlesById, childIdsByManager, visited))
                .Where(node => node is not null)
                .Select(node => node!)
                .OrderBy(node => node.FullName, StringComparer.Ordinal)
                .ToList()
            : [];

        return new OrgChartNodeDto(
            employeeId.Value,
            employee.Code.Value,
            employee.FirstName + " " + employee.LastName,
            employee.Status.ToString(),
            designationTitle,
            departmentName,
            children);
    }

    /// <summary>Hashes every fact that could change what this query renders: the employees'
    /// optimistic-concurrency <c>RowVersion</c> (bumped on every save) and the reporting
    /// relationships' own field values (there's no separate version stamp on
    /// <see cref="ReportingRelationship"/> — it isn't an <c>AggregateRoot</c> — so its fields are
    /// hashed directly; closing a line changes <c>ValidTo</c>, which changes the hash). A repeat
    /// request with nothing changed underneath produces the same tag; anything that would change
    /// the response changes it.</summary>
    private static string ComputeETag(
        TenantId tenantId, DateOnly asOf, Guid? rootEmployeeId, IReadOnlyList<Employee> employees, IReadOnlyList<ReportingRelationship> relationships)
    {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(tenantId.Value.ToByteArray());
            writer.Write(asOf.DayNumber);
            writer.Write(rootEmployeeId?.ToByteArray() ?? []);

            foreach (var employee in employees.OrderBy(e => e.Id.Value))
            {
                writer.Write(employee.Id.Value.ToByteArray());
                writer.Write(employee.RowVersion);
            }

            foreach (var relationship in relationships.OrderBy(rr => rr.Id.Value))
            {
                writer.Write(relationship.Id.Value.ToByteArray());
                writer.Write(relationship.EmployeeId.Value.ToByteArray());
                writer.Write(relationship.ManagerId.Value.ToByteArray());
                writer.Write(relationship.ValidFrom.DayNumber);
                writer.Write(relationship.ValidTo?.DayNumber ?? -1);
            }
        }

        return Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
    }
}
