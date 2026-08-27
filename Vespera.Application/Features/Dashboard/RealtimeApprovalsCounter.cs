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

        var count = 0;
        foreach (var chain in chains)
        {
            var nominalApproverId = chain.CurrentStep.ApproverId;
            var delegations = await proxyDelegations.ListIgnoringFiltersAsync(
                new ProxyDelegationsByDelegatorSpecification(tenantId, nominalApproverId), cancellationToken);
            var resolvedApproverId = resolver.ResolveApprover(nominalApproverId, asOf, delegations);

            if (resolvedApproverId == approverId)
            {
                count++;
            }
        }

        return count;
    }
}
