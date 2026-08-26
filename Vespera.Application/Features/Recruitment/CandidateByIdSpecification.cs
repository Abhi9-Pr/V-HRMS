using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class CandidateByIdSpecification : ISpecification<Candidate>
{
    public CandidateByIdSpecification(CandidateId candidateId)
    {
        Criteria = candidate => candidate.Id == candidateId;
    }

    public Expression<Func<Candidate, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Candidate, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Candidate, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
