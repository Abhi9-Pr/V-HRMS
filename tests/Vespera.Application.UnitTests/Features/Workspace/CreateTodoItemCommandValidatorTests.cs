using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class CreateTodoItemCommandValidatorTests
{
    private readonly CreateTodoItemCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var result = _validator.TestValidate(new CreateTodoItemCommand(string.Empty, null, TodoUrgency.Medium, null));

        result.ShouldHaveValidationErrorFor(command => command.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new CreateTodoItemCommand(new string('a', 257), null, TodoUrgency.Medium, null));

        result.ShouldHaveValidationErrorFor(command => command.Title);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new CreateTodoItemCommand("Submit timesheet", null, TodoUrgency.Medium, null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
