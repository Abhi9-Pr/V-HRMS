using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Leave;

/// <summary>
/// Notifies whoever needs to act next on a leave request's approval chain. Runs off the outbox with
/// no ambient tenant context — same reasoning and <see cref="IReadRepositoryAdmin{T}"/> pattern as
/// <c>RegularizationApprovedDomainEventHandler</c> — and is subject-type-agnostic at the
/// <see cref="ApprovalStepAssigned"/> level (the chain is reused by other subject types), so it
/// first filters to <see cref="ApprovalSubjectType.LeaveRequest"/> and no-ops otherwise. Resolves
/// the *actual* recipient (delegate, if one is active) rather than the nominal approver, so "Holiday
/// Mode" routes notifications the same way it routes the decision itself.
/// </summary>
public sealed class ApprovalStepAssignedNotificationHandler : INotificationHandler<DomainEventNotification<ApprovalStepAssigned>>
{
    private readonly IReadRepositoryAdmin<LeaveRequest> _leaveRequestsAdmin;
    private readonly IReadRepositoryAdmin<ProxyDelegation> _delegationsAdmin;
    private readonly IReadRepositoryAdmin<User> _usersAdmin;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ApprovalChainResolver _approvalChainResolver = new();

    public ApprovalStepAssignedNotificationHandler(
        IReadRepositoryAdmin<LeaveRequest> leaveRequestsAdmin, IReadRepositoryAdmin<ProxyDelegation> delegationsAdmin,
        IReadRepositoryAdmin<User> usersAdmin, INotificationDispatcher dispatcher)
    {
        _leaveRequestsAdmin = leaveRequestsAdmin;
        _delegationsAdmin = delegationsAdmin;
        _usersAdmin = usersAdmin;
        _dispatcher = dispatcher;
    }

    public async Task Handle(DomainEventNotification<ApprovalStepAssigned> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        if (domainEvent.SubjectType != ApprovalSubjectType.LeaveRequest)
        {
            return;
        }

        var tenantId = domainEvent.TenantId;
        var asOf = DateOnly.FromDateTime(domainEvent.OccurredOn.UtcDateTime);

        var leaveRequests = await _leaveRequestsAdmin.ListIgnoringFiltersAsync(
            new LeaveRequestByIdSpecification(tenantId, new LeaveRequestId(domainEvent.SubjectId)), cancellationToken);
        var leaveRequest = FirstOrNull(leaveRequests);
        if (leaveRequest is null)
        {
            return;
        }

        var delegations = (await _delegationsAdmin.ListIgnoringFiltersAsync(
                new DelegationsByDelegatorSpecification(domainEvent.ApproverId), cancellationToken))
            .Where(d => d.IsActiveOn(asOf) && d.Scope is DelegationScope.LeaveApprovals or DelegationScope.All)
            .ToList();
        var actualApproverId = _approvalChainResolver.ResolveApprover(domainEvent.ApproverId, asOf, delegations);

        var users = await _usersAdmin.ListIgnoringFiltersAsync(new UserByEmployeeIdSpecification(tenantId, actualApproverId), cancellationToken);
        var recipient = FirstOrNull(users);
        if (recipient is null)
        {
            return;
        }

        await _dispatcher.DispatchAsync(
            new NotificationMessage(
                recipient.Id.Value.ToString(),
                "Leave approval needed",
                $"A leave request ({leaveRequest.Period.Start:yyyy-MM-dd} to {leaveRequest.Period.End:yyyy-MM-dd}, " +
                $"{leaveRequest.RequestedDays} day(s)) needs your approval.",
                new Dictionary<string, string>
                {
                    ["email"] = recipient.Email.Value,
                    ["tenantId"] = tenantId.Value.ToString(),
                    ["entityType"] = "LeaveRequest",
                    ["entityId"] = leaveRequest.Id.Value.ToString(),
                    ["deepLink"] = $"vespera://leave/approvals/{leaveRequest.Id.Value}",
                }),
            cancellationToken);
    }

    private static T? FirstOrNull<T>(IReadOnlyList<T> items) where T : class => items.Count > 0 ? items[0] : null;
}
