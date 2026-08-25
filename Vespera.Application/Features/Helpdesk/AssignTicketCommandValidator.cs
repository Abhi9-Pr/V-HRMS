using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.EmployeeId).NotEmpty();
    }
}
