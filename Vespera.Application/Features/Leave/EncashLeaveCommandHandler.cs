using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class EncashLeaveCommandHandler : IRequestHandler<EncashLeaveCommand, Result>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<LeaveType> _leaveTypes;
    private readonly IReadRepository<LeaveBalance> _balances;
    private readonly IWriteRepository<LeaveBalance> _balanceWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EncashLeaveCommandHandler(
        IReadRepository<User> users, IReadRepository<LeaveType> leaveTypes, IReadRepository<LeaveBalance> balances,
        IWriteRepository<LeaveBalance> balanceWriter, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _leaveTypes = leaveTypes;
        _balances = balances;
        _balanceWriter = balanceWriter;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(EncashLeaveCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _dateTimeProvider.UtcNow;

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure(Error.Unauthorized("leave_encashment.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } employeeId)
        {
            return Result.Failure(Error.Validation("leave_encashment.no_employee_profile", "This user has no linked employee profile."));
        }

        var leaveTypeId = new LeaveTypeId(request.LeaveTypeId);
        var leaveType = await _leaveTypes.FirstOrDefaultAsync(new LeaveTypeByIdSpecification(tenantId, leaveTypeId), cancellationToken);
        if (leaveType is null)
        {
            return Result.Failure(Error.NotFound("leave_encashment.leave_type_not_found", "Leave type not found."));
        }

        if (!leaveType.IsEncashable)
        {
            return Result.Failure(Error.Validation("leave_encashment.not_encashable", "This leave type cannot be encashed."));
        }

        if (request.Days > leaveType.MaxEncashableDays)
        {
            return Result.Failure(Error.Validation(
                "leave_encashment.exceeds_maximum", $"At most {leaveType.MaxEncashableDays} day(s) may be encashed at once for this leave type."));
        }

        var balance = await _balances.FirstOrDefaultAsync(
            new LeaveBalanceByEmployeeAndTypeSpecification(tenantId, employeeId, leaveTypeId), cancellationToken);
        if (balance is null || request.Days > balance.Available)
        {
            return Result.Failure(Error.Conflict("leave_encashment.insufficient_balance", "Insufficient leave balance to encash."));
        }

        var result = balance.Encash(request.Days, now, callerUser.Email.Value);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        _balanceWriter.Update(balance);
        return Result.Success();
    }
}
