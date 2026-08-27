using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed class ProxyDelegationsByDelegatorSpecification : ISpecification<ProxyDelegation>
{
    public ProxyDelegationsByDelegatorSpecification(TenantId tenantId, EmployeeId delegatorId)
    {
        Criteria = delegation => delegation.TenantId == tenantId && delegation.DelegatorId == delegatorId && !delegation.IsRevoked;
    }

    public Expression<Func<ProxyDelegation, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<ProxyDelegation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ProxyDelegation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
