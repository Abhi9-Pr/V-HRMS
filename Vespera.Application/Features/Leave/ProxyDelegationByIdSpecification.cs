using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class ProxyDelegationByIdSpecification : ISpecification<ProxyDelegation>
{
    public ProxyDelegationByIdSpecification(TenantId tenantId, ProxyDelegationId id)
    {
        Criteria = d => d.TenantId == tenantId && d.Id == id;
    }

    public Expression<Func<ProxyDelegation, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ProxyDelegation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ProxyDelegation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
