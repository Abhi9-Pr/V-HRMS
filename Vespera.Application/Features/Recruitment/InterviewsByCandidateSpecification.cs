using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class InterviewsByCandidateSpecification : ISpecification<Interview>
{
    public InterviewsByCandidateSpecification(TenantId tenantId, CandidateId candidateId)
    {
        Criteria = interview => interview.TenantId == tenantId && interview.CandidateId == candidateId;
    }

    public Expression<Func<Interview, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Interview, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Interview, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
