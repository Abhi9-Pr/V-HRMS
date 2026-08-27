using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Dashboard;

public sealed class ApprovalChainByIdSpecification : ISpecification<ApprovalChain>
{
    public ApprovalChainByIdSpecification(TenantId tenantId, ApprovalChainId chainId)
    {
        Criteria = chain => chain.TenantId == tenantId && chain.Id == chainId;
    }

    public Expression<Func<ApprovalChain, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<ApprovalChain, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ApprovalChain, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
