using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;

namespace Vespera.Application.Features.Dashboard;

/// <summary><see cref="ApprovalChainApproved"/> doesn't carry who cast the final decision (unlike
/// <see cref="ApprovalChainRejected"/>, which carries <c>DecidedBy</c>), so this handler loads the
/// chain and takes the most recently decided step's approver.</summary>
public sealed class ApprovalChainApprovedDashboardHandler : INotificationHandler<DomainEventNotification<ApprovalChainApproved>>
{
    private readonly IReadRepositoryAdmin<ApprovalChain> _approvalChainsAdmin;
    private readonly IReadRepositoryAdmin<ProxyDelegation> _proxyDelegationsAdmin;
    private readonly IReadRepositoryAdmin<User> _usersAdmin;
    private readonly IDashboardRealtimePublisher _publisher;

    public ApprovalChainApprovedDashboardHandler(
        IReadRepositoryAdmin<ApprovalChain> approvalChainsAdmin, IReadRepositoryAdmin<ProxyDelegation> proxyDelegationsAdmin,
        IReadRepositoryAdmin<User> usersAdmin, IDashboardRealtimePublisher publisher)
    {
        _approvalChainsAdmin = approvalChainsAdmin;
        _proxyDelegationsAdmin = proxyDelegationsAdmin;
        _usersAdmin = usersAdmin;
        _publisher = publisher;
    }

    public async Task Handle(DomainEventNotification<ApprovalChainApproved> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var chains = await _approvalChainsAdmin.ListIgnoringFiltersAsync(
            new ApprovalChainByIdSpecification(domainEvent.TenantId, domainEvent.ChainId), cancellationToken);
        var chain = chains.Count > 0 ? chains[0] : null;
        var lastDecidedStep = chain?.Steps
            .Where(step => step.Status == ApprovalStepStatus.Approved && step.DecidedBy is not null)
            .OrderByDescending(step => step.DecidedAt)
            .FirstOrDefault();
        if (lastDecidedStep?.DecidedBy is not { } deciderId)
        {
            return;
        }

        var asOf = DateOnly.FromDateTime(domainEvent.OccurredOn.UtcDateTime);
        var count = await RealtimeApprovalsCounter.CountPendingForApproverAsync(
            _approvalChainsAdmin, _proxyDelegationsAdmin, domainEvent.TenantId, deciderId, asOf, cancellationToken);

        var users = await _usersAdmin.ListIgnoringFiltersAsync(
            new UserByEmployeeIdSpecification(domainEvent.TenantId, deciderId), cancellationToken);
        var recipient = users.Count > 0 ? users[0] : null;
        if (recipient is null)
        {
            return;
        }

        await _publisher.PublishApprovalsCountChangedAsync(recipient.Id.Value, new ApprovalsCountChangedPayload(count), cancellationToken);
    }
}
