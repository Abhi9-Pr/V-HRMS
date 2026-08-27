using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class CancelCorporateEventCommandValidator : AbstractValidator<CancelCorporateEventCommand>
{
    public CancelCorporateEventCommandValidator()
    {
        RuleFor(command => command.CorporateEventId).NotEmpty();
    }
}
