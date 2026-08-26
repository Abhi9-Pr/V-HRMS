using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class AcceptOfferCommandValidator : AbstractValidator<AcceptOfferCommand>
{
    public AcceptOfferCommandValidator()
    {
        RuleFor(command => command.OfferLetterId).NotEmpty();
    }
}
