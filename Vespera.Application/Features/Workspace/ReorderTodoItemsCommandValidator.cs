using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class ReorderTodoItemsCommandValidator : AbstractValidator<ReorderTodoItemsCommand>
{
    public ReorderTodoItemsCommandValidator()
    {
        RuleFor(command => command.OrderedTodoItemIds).NotNull();
        RuleForEach(command => command.OrderedTodoItemIds).NotEmpty();
    }
}
