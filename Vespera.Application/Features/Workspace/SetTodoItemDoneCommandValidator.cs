using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class SetTodoItemDoneCommandValidator : AbstractValidator<SetTodoItemDoneCommand>
{
    public SetTodoItemDoneCommandValidator()
    {
        RuleFor(command => command.TodoItemId).NotEmpty();
    }
}
