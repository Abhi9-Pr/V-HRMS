using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class WithdrawLeaveRequestCommandHandler : IRequestHandler<WithdrawLeaveRequestCommand, Result>
{
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly IWriteRepository<LeaveRequest> _leaveRequestWriter;
    private readonly IReadRepository<ApprovalChain> _chains;
    private readonly IWriteRepository<ApprovalChain> _chainWriter;
    private readonly IReadRepository<LeaveBalance> _balances;
    private readonly IWriteRepository<LeaveBalance> _balanceWriter;
    private readonly IReadRepository<User> _users;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WithdrawLeaveRequestCommandHandler(
        IReadRepository<LeaveRequest> leaveRequests, IWriteRepository<LeaveRequest> leaveRequestWriter,
        IReadRepository<ApprovalChain> chains, IWriteRepository<ApprovalChain> chainWriter,
        IReadRepository<LeaveBalance> balances, IWriteRepository<LeaveBalance> balanceWriter, IReadRepository<User> users,
        ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _leaveRequests = leaveRequests;
        _leaveRequestWriter = leaveRequestWriter;
        _chains = chains;
        _chainWriter = chainWriter;
        _balances = balances;
        _balanceWriter = balanceWriter;
        _users = users;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(WithdrawLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _dateTimeProvider.UtcNow;

        var leaveRequest = await _leaveRequests.FirstOrDefaultAsync(
            new LeaveRequestByIdSpecification(tenantId, new LeaveRequestId(request.LeaveRequestId)), cancellationToken);
        if (leaveRequest is null)
        {
            return Result.Failure(Error.NotFound("leave_request.not_found", "Leave request not found."));
        }

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure(Error.Unauthorized("leave_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is null || callerUser.EmployeeId != leaveRequest.EmployeeId)
        {
            return Result.Failure(Error.Forbidden("leave_request.not_the_requester", "Only the requester can withdraw this request."));
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("leave_request.not_pending", "Only a pending request can be withdrawn."));
        }

        var cancelResult = leaveRequest.Cancel(now, callerUser.Email.Value);
        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        var chain = await _chains.FirstOrDefaultAsync(
            new ApprovalChainBySubjectSpecification(tenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value), cancellationToken);
        if (chain is { Status: ApprovalChainStatus.InProgress })
        {
            var chainCancelResult = chain.Cancel();
            if (chainCancelResult.IsFailure)
            {
                return chainCancelResult;
            }

            _chainWriter.Update(chain);
        }

        var balance = await _balances.FirstOrDefaultAsync(
            new LeaveBalanceByEmployeeAndTypeSpecification(tenantId, leaveRequest.EmployeeId, leaveRequest.LeaveTypeId), cancellationToken);
        if (balance is not null)
        {
            var reversalResult = LeaveLedgerReverser.ReverseDebit(balance, leaveRequest.Id, "Withdrawn", now, callerUser.Email.Value);
            if (reversalResult.IsFailure)
            {
                return reversalResult;
            }

            _balanceWriter.Update(balance);
        }

        _leaveRequestWriter.Update(leaveRequest);
        return Result.Success();
    }
}
