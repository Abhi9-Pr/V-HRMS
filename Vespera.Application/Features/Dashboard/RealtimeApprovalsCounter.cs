using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Dashboard;

/// <summary>Shared by the three approval-chain domain-event handlers that push an updated pending-
/// approvals count over SignalR (<c>ApprovalStepAssignedDashboardHandler</c>,
/// <c>ApprovalChainApprovedDashboardHandler</c>, <c>ApprovalChainRejectedDashboardHandler</c>).
/// Uses <see cref="IReadRepositoryAdmin{T}"/> because these run off the outbox with no ambient
/// tenant context — the same reasoning as <c>ApprovalStepAssignedNotificationHandler</c>. Not
/// reused by <c>PendingApprovalsWidgetProvider</c>, which runs inside an authenticated request and
/// so uses the ordinary tenant-scoped <see cref="IReadRepository{T}"/> instead.</summary>
public static class RealtimeApprovalsCounter
{
    public static async Task<int> CountPendingForApproverAsync(
        IReadRepositoryAdmin<ApprovalChain> approvalChains, IReadRepositoryAdmin<ProxyDelegation> proxyDelegations,
        TenantId tenantId, EmployeeId approverId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var chains = await approvalChains.ListIgnoringFiltersAsync(
            new InProgressApprovalChainsSpecification(tenantId), cancellationToken);
        var resolver = new ApprovalChainResolver();

        // One query for every chain's nominal approver instead of one per chain — this runs on
        // every approval-chain domain event (assigned/approved/rejected), not just once per
        // dashboard load, so the per-chain round-trip here was the hotter of the two identical
        // N+1 shapes in this feature area (see PendingApprovalsWidgetProvider's own fix).
        var nominalApproverIds = chains.Select(chain => chain.CurrentStep.ApproverId).Distinct().ToList();
        var delegationsByDelegator = (await proxyDelegations.ListIgnoringFiltersAsync(
                new ProxyDelegationsByDelegatorsSpecification(tenantId, nominalApproverIds), cancellationToken))
            .GroupBy(delegation => delegation.DelegatorId)
            .ToDictionary(group => group.Key, group => (IEnumerable<ProxyDelegation>)group);

        var count = 0;
        foreach (var chain in chains)
        {
            var nominalApproverId = chain.CurrentStep.ApproverId;
            var delegations = delegationsByDelegator.GetValueOrDefault(nominalApproverId, []);
            var resolvedApproverId = resolver.ResolveApprover(nominalApproverId, asOf, delegations);

            if (resolvedApproverId == approverId)
            {
                count++;
            }
        }

        return count;
    }
}
