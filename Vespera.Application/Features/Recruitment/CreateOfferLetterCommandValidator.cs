using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class CreateOfferLetterCommandValidator : AbstractValidator<CreateOfferLetterCommand>
{
    public CreateOfferLetterCommandValidator()
    {
        RuleFor(command => command.CandidateId).NotEmpty();
        RuleFor(command => command.ProposedDesignationId).NotEmpty();
        RuleFor(command => command.ProposedCtc).GreaterThan(0);
        RuleFor(command => command.Currency).IsInEnum();
    }
}
