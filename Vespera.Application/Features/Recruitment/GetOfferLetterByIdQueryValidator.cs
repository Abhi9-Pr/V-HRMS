using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetOfferLetterByIdQueryValidator : AbstractValidator<GetOfferLetterByIdQuery>
{
    public GetOfferLetterByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
