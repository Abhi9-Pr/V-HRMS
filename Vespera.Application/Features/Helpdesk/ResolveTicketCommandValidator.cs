using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class ResolveTicketCommandValidator : AbstractValidator<ResolveTicketCommand>
{
    public ResolveTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
    }
}
