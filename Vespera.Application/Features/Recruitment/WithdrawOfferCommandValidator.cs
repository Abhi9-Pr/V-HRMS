using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class WithdrawOfferCommandValidator : AbstractValidator<WithdrawOfferCommand>
{
    public WithdrawOfferCommandValidator()
    {
        RuleFor(command => command.OfferLetterId).NotEmpty();
    }
}
