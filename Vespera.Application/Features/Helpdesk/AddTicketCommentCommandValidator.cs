using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class AddTicketCommentCommandValidator : AbstractValidator<AddTicketCommentCommand>
{
    public AddTicketCommentCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Body).NotEmpty().MaximumLength(4096);
    }
}
