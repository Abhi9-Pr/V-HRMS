using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetInterviewsForCandidateQueryValidator : AbstractValidator<GetInterviewsForCandidateQuery>
{
    public GetInterviewsForCandidateQueryValidator()
    {
        RuleFor(query => query.CandidateId).NotEmpty();
    }
}
