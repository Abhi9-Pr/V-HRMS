using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class SetTodoItemUrgencyCommandValidatorTests
{
    private readonly SetTodoItemUrgencyCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_TodoItemId_Is_Empty()
    {
        var result = _validator.TestValidate(new SetTodoItemUrgencyCommand(Guid.Empty, TodoUrgency.Medium));

        result.ShouldHaveValidationErrorFor(command => command.TodoItemId);
    }

    [Fact]
    public void Should_Have_Error_When_Urgency_Is_Not_A_Defined_Enum_Value()
    {
        var result = _validator.TestValidate(new SetTodoItemUrgencyCommand(Guid.NewGuid(), (TodoUrgency)999));

        result.ShouldHaveValidationErrorFor(command => command.Urgency);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new SetTodoItemUrgencyCommand(Guid.NewGuid(), TodoUrgency.High));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
