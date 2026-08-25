using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class CreateReportingRelationshipCommandHandler : IRequestHandler<CreateReportingRelationshipCommand, Result<Guid>>
{
    private const int MaxChainDepth = 200;

    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;
    private readonly IWriteRepository<ReportingRelationship> _reportingRelationshipWriter;
    private readonly ITenantContext _tenantContext;

    public CreateReportingRelationshipCommandHandler(
        IReadRepository<Employee> employees,
        IReadRepository<ReportingRelationship> reportingRelationships,
        IWriteRepository<ReportingRelationship> reportingRelationshipWriter,
        ITenantContext tenantContext)
    {
        _employees = employees;
        _reportingRelationships = reportingRelationships;
        _reportingRelationshipWriter = reportingRelationshipWriter;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateReportingRelationshipCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var employeeId = new EmployeeId(request.EmployeeId);
        var managerId = new EmployeeId(request.ManagerId);

        var employeeExists = await _employees.AnyAsync(
            new EmployeeByIdSpecification(tenantId, employeeId), cancellationToken);
        if (!employeeExists)
        {
            return Result.Failure<Guid>(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var managerExists = await _employees.AnyAsync(
            new EmployeeByIdSpecification(tenantId, managerId), cancellationToken);
        if (!managerExists)
        {
            return Result.Failure<Guid>(Error.NotFound("employee.not_found", "Manager not found."));
        }

        var createResult = ReportingRelationship.Create(tenantId, employeeId, managerId, request.ValidFrom, request.ValidTo);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var candidate = createResult.Value;

        var existing = await _reportingRelationships.ListAsync(
            new ReportingRelationshipsByEmployeeSpecification(employeeId), cancellationToken);

        var overlapResult = EffectiveDatedTimeline.EnsureNoOverlap<ReportingRelationshipId, ReportingRelationship>(existing, candidate);
        if (overlapResult.IsFailure)
        {
            return Result.Failure<Guid>(overlapResult.Error);
        }

        var cycleResult = await DetectCycleAsync(employeeId, managerId, request.ValidFrom, cancellationToken);
        if (cycleResult.IsFailure)
        {
            return Result.Failure<Guid>(cycleResult.Error);
        }

        await _reportingRelationshipWriter.AddAsync(candidate, cancellationToken);
        return Result.Success(candidate.Id.Value);
    }

    /// <summary>Walks the manager chain starting at <paramref name="managerId"/> (the proposed new
    /// manager), following "who manages this person" one hop at a time. If <paramref name="employeeId"/>
    /// (the person about to be assigned this manager) ever turns up as an ancestor, assigning
    /// <paramref name="managerId"/> as their manager would close a cycle. Capped at
    /// <see cref="MaxChainDepth"/> hops so pre-existing bad data can never turn this into an
    /// infinite loop.</summary>
    private async Task<Result> DetectCycleAsync(EmployeeId employeeId, EmployeeId managerId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var currentId = managerId;

        for (var hop = 0; hop < MaxChainDepth; hop++)
        {
            var relationship = await _reportingRelationships.FirstOrDefaultAsync(
                new ActiveManagerRelationshipSpecification(currentId, asOf), cancellationToken);

            if (relationship is null)
            {
                return Result.Success();
            }

            if (relationship.ManagerId == employeeId)
            {
                return Result.Failure(Error.Validation(
                    "reporting_relationship.cycle_detected",
                    "This reporting line would create a cycle in the management chain."));
            }

            currentId = relationship.ManagerId;
        }

        return Result.Failure(Error.Validation(
            "reporting_relationship.cycle_detected",
            "The management chain exceeds the maximum supported depth; refusing to create a potential cycle."));
    }
}
