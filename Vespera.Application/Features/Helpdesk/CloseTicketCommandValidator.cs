using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CloseTicketCommandValidator : AbstractValidator<CloseTicketCommand>
{
    public CloseTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
    }
}
