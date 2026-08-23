using FluentValidation.TestHelper;
using Vespera.Application.Features.Designations;

namespace Vespera.Application.UnitTests.Features.Designations;

public class UpdateDesignationCommandValidatorTests
{
    private readonly UpdateDesignationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new UpdateDesignationCommand(Guid.Empty, "Software Engineer", 3);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var command = new UpdateDesignationCommand(Guid.NewGuid(), string.Empty, 3);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Grade_Is_Not_Positive()
    {
        var command = new UpdateDesignationCommand(Guid.NewGuid(), "Software Engineer", 0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Grade);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UpdateDesignationCommand(Guid.NewGuid(), "Software Engineer", 3);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
