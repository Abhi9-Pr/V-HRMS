using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(256);
    }
}
