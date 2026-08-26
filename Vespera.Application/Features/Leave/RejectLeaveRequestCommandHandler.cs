using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, Result>
{
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly IWriteRepository<LeaveRequest> _leaveRequestWriter;
    private readonly IReadRepository<ApprovalChain> _chains;
    private readonly IWriteRepository<ApprovalChain> _chainWriter;
    private readonly IReadRepository<LeaveBalance> _balances;
    private readonly IWriteRepository<LeaveBalance> _balanceWriter;
    private readonly IReadRepository<User> _users;
    private readonly LeaveApprovalStepAuthorizer _authorizer;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RejectLeaveRequestCommandHandler(
        IReadRepository<LeaveRequest> leaveRequests, IWriteRepository<LeaveRequest> leaveRequestWriter,
        IReadRepository<ApprovalChain> chains, IWriteRepository<ApprovalChain> chainWriter,
        IReadRepository<LeaveBalance> balances, IWriteRepository<LeaveBalance> balanceWriter, IReadRepository<User> users,
        LeaveApprovalStepAuthorizer authorizer, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _leaveRequests = leaveRequests;
        _leaveRequestWriter = leaveRequestWriter;
        _chains = chains;
        _chainWriter = chainWriter;
        _balances = balances;
        _balanceWriter = balanceWriter;
        _users = users;
        _authorizer = authorizer;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var leaveRequest = await _leaveRequests.FirstOrDefaultAsync(
            new LeaveRequestByIdSpecification(tenantId, new LeaveRequestId(request.LeaveRequestId)), cancellationToken);
        if (leaveRequest is null)
        {
            return Result.Failure(Error.NotFound("leave_request.not_found", "Leave request not found."));
        }

        var chain = await _chains.FirstOrDefaultAsync(
            new ApprovalChainBySubjectSpecification(tenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value), cancellationToken);
        if (chain is null)
        {
            return Result.Failure(Error.NotFound("leave_request.chain_not_found", "No approval chain exists for this request."));
        }

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure(Error.Unauthorized("leave_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        var authorizedApproverId = chain.Status == ApprovalChainStatus.InProgress
            ? await _authorizer.ResolveAuthorizedApproverAsync(chain.CurrentStep.ApproverId, today, cancellationToken)
            : (EmployeeId?)null;

        if (callerUser?.EmployeeId is null || authorizedApproverId is null || callerUser.EmployeeId != authorizedApproverId)
        {
            return Result.Failure(Error.Forbidden("leave_request.not_authorized_approver", "You are not authorized to reject this request."));
        }

        var chainResult = chain.Reject(callerUser.EmployeeId.Value, now, request.Reason);
        if (chainResult.IsFailure)
        {
            return chainResult;
        }

        var rejectResult = leaveRequest.Reject(callerUser.EmployeeId.Value, request.Reason, now, callerUser.Email.Value);
        if (rejectResult.IsFailure)
        {
            return rejectResult;
        }

        var balance = await _balances.FirstOrDefaultAsync(
            new LeaveBalanceByEmployeeAndTypeSpecification(tenantId, leaveRequest.EmployeeId, leaveRequest.LeaveTypeId), cancellationToken);
        if (balance is not null)
        {
            var reversalResult = LeaveLedgerReverser.ReverseDebit(balance, leaveRequest.Id, "Rejected", now, callerUser.Email.Value);
            if (reversalResult.IsFailure)
            {
                return reversalResult;
            }

            _balanceWriter.Update(balance);
        }

        _leaveRequestWriter.Update(leaveRequest);
        _chainWriter.Update(chain);
        return Result.Success();
    }
}
