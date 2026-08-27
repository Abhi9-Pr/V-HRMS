using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class RespondToEventRsvpCommandValidator : AbstractValidator<RespondToEventRsvpCommand>
{
    public RespondToEventRsvpCommandValidator()
    {
        RuleFor(command => command.CorporateEventId).NotEmpty();
        RuleFor(command => command.Response).IsInEnum();
    }
}
