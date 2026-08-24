using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class SendOfferLetterCommandValidator : AbstractValidator<SendOfferLetterCommand>
{
    public SendOfferLetterCommandValidator()
    {
        RuleFor(command => command.OfferLetterId).NotEmpty();
    }
}
