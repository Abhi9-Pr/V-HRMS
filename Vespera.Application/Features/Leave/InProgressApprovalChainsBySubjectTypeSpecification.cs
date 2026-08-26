using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class InProgressApprovalChainsBySubjectTypeSpecification : ISpecification<ApprovalChain>
{
    public InProgressApprovalChainsBySubjectTypeSpecification(TenantId tenantId, ApprovalSubjectType subjectType)
    {
        Criteria = c => c.TenantId == tenantId && c.SubjectType == subjectType && c.Status == ApprovalChainStatus.InProgress;
    }

    public Expression<Func<ApprovalChain, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ApprovalChain, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ApprovalChain, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
