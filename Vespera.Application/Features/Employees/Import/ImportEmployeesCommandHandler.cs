using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.OrgChart;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Employees.Import;

public sealed class ImportEmployeesCommandHandler : IRequestHandler<ImportEmployeesCommand, Result<BulkImportReportDto>>
{
    private readonly IReadRepository<Department> _departments;
    private readonly IReadRepository<Designation> _designations;
    private readonly IReadRepository<Location> _locations;
    private readonly IReadRepository<Employee> _existingEmployees;
    private readonly IWriteRepository<Employee> _employeeWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ImportEmployeesCommandHandler(
        IReadRepository<Department> departments,
        IReadRepository<Designation> designations,
        IReadRepository<Location> locations,
        IReadRepository<Employee> existingEmployees,
        IWriteRepository<Employee> employeeWriter,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _departments = departments;
        _designations = designations;
        _locations = locations;
        _existingEmployees = existingEmployees;
        _employeeWriter = employeeWriter;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<BulkImportReportDto>> Handle(ImportEmployeesCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var departmentsByCode = (await _departments.ListAsync(new DepartmentsByTenantSpecification(tenantId), cancellationToken))
            .ToDictionary(d => d.Code, d => d.Id, StringComparer.OrdinalIgnoreCase);
        var designationsByTitle = (await _designations.ListAsync(new DesignationsByTenantSpecification(tenantId), cancellationToken))
            .ToDictionary(d => d.Title, d => d.Id, StringComparer.OrdinalIgnoreCase);
        var locationsByName = (await _locations.ListAsync(new LocationsByTenantSpecification(tenantId), cancellationToken))
            .ToDictionary(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase);
        var existingCodes = (await _existingEmployees.ListAsync(new EmployeesByTenantSpecification(tenantId), cancellationToken))
            .Select(e => e.Code.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";

        var rowResults = new List<BulkImportRowResult>();
        var toCreate = new List<Employee>();
        var codesSeenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in request.Rows)
        {
            var errors = new List<string>();

            if (row.DateOfBirth >= row.DateOfJoining)
            {
                errors.Add("Date of birth must be before date of joining.");
            }

            if (existingCodes.Contains(row.Code) || !codesSeenInBatch.Add(row.Code))
            {
                errors.Add($"Employee code '{row.Code}' is already in use.");
            }

            if (!departmentsByCode.TryGetValue(row.DepartmentCode, out var departmentId))
            {
                errors.Add($"Department code '{row.DepartmentCode}' does not exist.");
            }

            if (!designationsByTitle.TryGetValue(row.DesignationTitle, out var designationId))
            {
                errors.Add($"Designation '{row.DesignationTitle}' does not exist.");
            }

            if (!locationsByName.TryGetValue(row.LocationName, out var locationId))
            {
                errors.Add($"Location '{row.LocationName}' does not exist.");
            }

            var code = EmployeeCode.Create(row.Code);
            if (code.IsFailure)
            {
                errors.Add(code.Error.Message);
            }

            var email = EmailAddress.Create(row.WorkEmail);
            if (email.IsFailure)
            {
                errors.Add(email.Error.Message);
            }

            var phone = PhoneNumber.Create(row.Phone);
            if (phone.IsFailure)
            {
                errors.Add(phone.Error.Message);
            }

            Employee? employee = null;
            if (errors.Count == 0)
            {
                var onboardResult = Employee.Onboard(
                    tenantId, code.Value, row.FirstName, row.LastName, email.Value, phone.Value,
                    row.DateOfBirth, row.DateOfJoining, departmentId, designationId, locationId,
                    now, createdBy);

                if (onboardResult.IsFailure)
                {
                    errors.Add(onboardResult.Error.Message);
                }
                else
                {
                    employee = onboardResult.Value;
                }
            }

            rowResults.Add(new BulkImportRowResult(row.RowNumber, errors.Count == 0, errors));
            if (employee is not null)
            {
                toCreate.Add(employee);
            }
        }

        var successCount = rowResults.Count(r => r.Success);
        var failureCount = rowResults.Count - successCount;

        if (request.DryRun || failureCount > 0)
        {
            return Result.Success(new BulkImportReportDto(rowResults, successCount, failureCount, Committed: false));
        }

        foreach (var employee in toCreate)
        {
            await _employeeWriter.AddAsync(employee, cancellationToken);
        }

        return Result.Success(new BulkImportReportDto(rowResults, successCount, failureCount, Committed: true));
    }
}
