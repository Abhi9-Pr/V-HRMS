using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class ExitEmployeeCommandHandler : IRequestHandler<ExitEmployeeCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ExitEmployeeCommandHandler(
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

    public async Task<Result> Handle(ExitEmployeeCommand request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.Id));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("employee.not_found", "Employee not found."));
        }

        if (!Enum.TryParse<EmployeeExitReason>(request.Reason, ignoreCase: true, out var reason))
        {
            return Result.Failure(Error.Validation("employee.invalid_exit_reason", "Unrecognized exit reason."));
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        return employee.Exit(request.ExitDate, reason, now, modifiedBy);
    }
}
