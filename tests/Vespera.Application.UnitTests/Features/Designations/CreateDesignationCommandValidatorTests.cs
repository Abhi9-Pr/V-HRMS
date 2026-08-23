using FluentValidation.TestHelper;
using Vespera.Application.Features.Designations;

namespace Vespera.Application.UnitTests.Features.Designations;

public class CreateDesignationCommandValidatorTests
{
    private readonly CreateDesignationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var command = new CreateDesignationCommand(string.Empty, 3, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Grade_Is_Not_Positive()
    {
        var command = new CreateDesignationCommand("Software Engineer", 0, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Grade);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateDesignationCommand("Software Engineer", 3, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
