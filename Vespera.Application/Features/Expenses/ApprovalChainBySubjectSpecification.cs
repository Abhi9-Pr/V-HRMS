using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Expenses;

public sealed class ApprovalChainBySubjectSpecification : ISpecification<ApprovalChain>
{
    public ApprovalChainBySubjectSpecification(TenantId tenantId, ApprovalSubjectType subjectType, Guid subjectId)
    {
        Criteria = chain => chain.TenantId == tenantId && chain.SubjectType == subjectType && chain.SubjectId == subjectId;
    }

    public Expression<Func<ApprovalChain, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ApprovalChain, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ApprovalChain, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
