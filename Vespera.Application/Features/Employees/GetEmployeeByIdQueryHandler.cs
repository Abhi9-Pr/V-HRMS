using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;

    public GetEmployeeByIdQueryHandler(IReadRepository<Employee> employees, ITenantContext tenantContext)
    {
        _employees = employees;
        _tenantContext = tenantContext;
    }

    public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.Id));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeDto>(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var asOfAssignment = request.AsOf is { } asOf ? EmploymentHistoryTimeline.AsOf(employee.EmploymentHistory, asOf) : null;

        var departmentId = asOfAssignment?.DepartmentId ?? employee.DepartmentId;
        var designationId = asOfAssignment?.DesignationId ?? employee.DesignationId;
        var locationId = asOfAssignment?.LocationId ?? employee.LocationId;

        var dto = new EmployeeDto(
            employee.Id.Value,
            employee.Code.Value,
            employee.FirstName,
            employee.LastName,
            employee.WorkEmail.Value,
            employee.Phone.Value,
            employee.DateOfBirth,
            employee.DateOfJoining,
            departmentId.Value,
            designationId.Value,
            locationId.Value,
            employee.Status.ToString(),
            employee.ExitDate,
            employee.ExitReason?.ToString(),
            employee.Pan?.Masked(),
            employee.BankAccount?.Masked(),
            employee.CurrentAnnualCtc is not null);

        return Result.Success(dto);
    }
}
