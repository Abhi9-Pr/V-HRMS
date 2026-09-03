using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Dashboard.Widgets;

/// <summary>Batch counterpart to <see cref="ProxyDelegationsByDelegatorSpecification"/> — one
/// query for every in-progress approval chain's nominal approver, instead of one query per chain.
/// See <see cref="PendingApprovalsWidgetProvider"/>.</summary>
public sealed class ProxyDelegationsByDelegatorsSpecification : ISpecification<ProxyDelegation>
{
    public ProxyDelegationsByDelegatorsSpecification(TenantId tenantId, IReadOnlyCollection<EmployeeId> delegatorIds)
    {
        Criteria = delegation =>
            delegation.TenantId == tenantId && delegatorIds.Contains(delegation.DelegatorId) && !delegation.IsRevoked;
    }

    public Expression<Func<ProxyDelegation, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<ProxyDelegation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ProxyDelegation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
