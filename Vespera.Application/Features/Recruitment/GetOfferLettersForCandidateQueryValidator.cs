using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetOfferLettersForCandidateQueryValidator : AbstractValidator<GetOfferLettersForCandidateQuery>
{
    public GetOfferLettersForCandidateQueryValidator()
    {
        RuleFor(query => query.CandidateId).NotEmpty();
    }
}
