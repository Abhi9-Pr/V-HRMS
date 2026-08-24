using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class InterviewByIdSpecification : ISpecification<Interview>
{
    public InterviewByIdSpecification(InterviewId interviewId)
    {
        Criteria = interview => interview.Id == interviewId;
    }

    public Expression<Func<Interview, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Interview, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Interview, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
