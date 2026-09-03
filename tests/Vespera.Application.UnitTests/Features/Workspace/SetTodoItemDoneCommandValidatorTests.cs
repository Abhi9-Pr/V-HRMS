using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class SetTodoItemDoneCommandValidatorTests
{
    private readonly SetTodoItemDoneCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_TodoItemId_Is_Empty()
    {
        var result = _validator.TestValidate(new SetTodoItemDoneCommand(Guid.Empty, true));

        result.ShouldHaveValidationErrorFor(command => command.TodoItemId);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new SetTodoItemDoneCommand(Guid.NewGuid(), true));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
