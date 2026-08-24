using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class RateTicketSatisfactionCommandValidator : AbstractValidator<RateTicketSatisfactionCommand>
{
    public RateTicketSatisfactionCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
    }
}
