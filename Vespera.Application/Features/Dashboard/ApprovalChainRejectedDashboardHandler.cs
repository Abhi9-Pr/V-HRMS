using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;

namespace Vespera.Application.Features.Dashboard;

/// <summary>Pushes the pending-approvals widget's updated count to whoever just rejected a
/// request — <see cref="ApprovalChainRejected.DecidedBy"/> is already the resolved (delegate-
/// aware) actor, so no delegation resolution is needed here, unlike
/// <see cref="ApprovalStepAssignedDashboardHandler"/>.</summary>
public sealed class ApprovalChainRejectedDashboardHandler : INotificationHandler<DomainEventNotification<ApprovalChainRejected>>
{
    private readonly IReadRepositoryAdmin<ApprovalChain> _approvalChainsAdmin;
    private readonly IReadRepositoryAdmin<ProxyDelegation> _proxyDelegationsAdmin;
    private readonly IReadRepositoryAdmin<User> _usersAdmin;
    private readonly IDashboardRealtimePublisher _publisher;

    public ApprovalChainRejectedDashboardHandler(
        IReadRepositoryAdmin<ApprovalChain> approvalChainsAdmin, IReadRepositoryAdmin<ProxyDelegation> proxyDelegationsAdmin,
        IReadRepositoryAdmin<User> usersAdmin, IDashboardRealtimePublisher publisher)
    {
        _approvalChainsAdmin = approvalChainsAdmin;
        _proxyDelegationsAdmin = proxyDelegationsAdmin;
        _usersAdmin = usersAdmin;
        _publisher = publisher;
    }

    public async Task Handle(DomainEventNotification<ApprovalChainRejected> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var asOf = DateOnly.FromDateTime(domainEvent.OccurredOn.UtcDateTime);

        var count = await RealtimeApprovalsCounter.CountPendingForApproverAsync(
            _approvalChainsAdmin, _proxyDelegationsAdmin, domainEvent.TenantId, domainEvent.DecidedBy, asOf, cancellationToken);

        var users = await _usersAdmin.ListIgnoringFiltersAsync(
            new UserByEmployeeIdSpecification(domainEvent.TenantId, domainEvent.DecidedBy), cancellationToken);
        var recipient = users.Count > 0 ? users[0] : null;
        if (recipient is null)
        {
            return;
        }

        await _publisher.PublishApprovalsCountChangedAsync(recipient.Id.Value, new ApprovalsCountChangedPayload(count), cancellationToken);
    }
}
