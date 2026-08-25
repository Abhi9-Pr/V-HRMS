using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class DeclineOfferCommandValidator : AbstractValidator<DeclineOfferCommand>
{
    public DeclineOfferCommandValidator()
    {
        RuleFor(command => command.OfferLetterId).NotEmpty();
    }
}
