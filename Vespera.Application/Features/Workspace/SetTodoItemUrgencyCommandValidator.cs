using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class SetTodoItemUrgencyCommandValidator : AbstractValidator<SetTodoItemUrgencyCommand>
{
    public SetTodoItemUrgencyCommandValidator()
    {
        RuleFor(command => command.TodoItemId).NotEmpty();
        RuleFor(command => command.Urgency).IsInEnum();
    }
}
