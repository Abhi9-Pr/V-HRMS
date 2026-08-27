using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Dashboard.Widgets;

/// <summary>Every in-progress approval chain for a tenant, across every
/// <see cref="ApprovalSubjectType"/> — unlike <c>PendingApprovalChainsSpecification</c>
/// (Expenses), which narrows to one subject type for that feature's own list screen, the pending-
/// approvals widget counts across all of them.</summary>
public sealed class InProgressApprovalChainsSpecification : ISpecification<ApprovalChain>
{
    public InProgressApprovalChainsSpecification(TenantId tenantId)
    {
        Criteria = chain => chain.TenantId == tenantId && chain.Status == ApprovalChainStatus.InProgress;
    }

    public Expression<Func<ApprovalChain, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<ApprovalChain, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ApprovalChain, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
