using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Expenses;

public sealed class PendingApprovalChainsSpecification : ISpecification<ApprovalChain>
{
    public PendingApprovalChainsSpecification(TenantId tenantId, ApprovalSubjectType subjectType)
    {
        Criteria = chain =>
            chain.TenantId == tenantId && chain.SubjectType == subjectType && chain.Status == ApprovalChainStatus.InProgress;
    }

    public Expression<Func<ApprovalChain, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ApprovalChain, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ApprovalChain, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
