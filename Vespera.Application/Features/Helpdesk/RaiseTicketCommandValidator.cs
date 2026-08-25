using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class RaiseTicketCommandValidator : AbstractValidator<RaiseTicketCommand>
{
    public RaiseTicketCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Subject).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Description).MaximumLength(4096);
        RuleFor(command => command.Priority).IsInEnum();
    }
}
