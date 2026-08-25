using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class TransferEmployeeCommandHandler : IRequestHandler<TransferEmployeeCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TransferEmployeeCommandHandler(
        IReadRepository<Employee> employees,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _employees = employees;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(TransferEmployeeCommand request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.Id));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("employee.not_found", "Employee not found."));
        }

        if (!Enum.TryParse<EmploymentChangeReason>(request.Reason, ignoreCase: true, out var reason))
        {
            return Result.Failure(Error.Validation("employee.invalid_change_reason", "Unrecognized transfer reason."));
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        return employee.Transfer(
            new DepartmentId(request.DepartmentId), new DesignationId(request.DesignationId), new LocationId(request.LocationId),
            request.EffectiveDate, reason, now, modifiedBy);
    }
}
