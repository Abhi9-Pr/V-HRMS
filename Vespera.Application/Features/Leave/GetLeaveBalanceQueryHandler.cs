using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetLeaveBalanceQueryHandler : IRequestHandler<GetLeaveBalanceQuery, Result<LeaveBalanceDto>>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<LeaveBalance> _balances;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public GetLeaveBalanceQueryHandler(
        IReadRepository<User> users, IReadRepository<LeaveBalance> balances, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _users = users;
        _balances = balances;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result<LeaveBalanceDto>> Handle(GetLeaveBalanceQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<LeaveBalanceDto>(Error.Unauthorized("leave_balance.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } employeeId)
        {
            return Result.Success(new LeaveBalanceDto(0m, 0m, 0m, 0m, []));
        }

        var balance = await _balances.FirstOrDefaultAsync(
            new LeaveBalanceByEmployeeAndTypeSpecification(_tenantContext.TenantId, employeeId, new LeaveTypeId(request.LeaveTypeId)),
            cancellationToken);
        if (balance is null)
        {
            return Result.Success(new LeaveBalanceDto(0m, 0m, 0m, 0m, []));
        }

        var recentEntries = balance.Entries
            .OrderByDescending(e => e.OccurredOn)
            .Take(20)
            .Select(e => new LeaveLedgerEntryDto(e.Id.Value, e.Type.ToString(), e.Direction.ToString(), e.Amount, e.Reason, e.OccurredOn, e.PostedBy))
            .ToList();

        return Result.Success(new LeaveBalanceDto(balance.Available, balance.Accrued, balance.Used, balance.CarriedForward, recentEntries));
    }
}
