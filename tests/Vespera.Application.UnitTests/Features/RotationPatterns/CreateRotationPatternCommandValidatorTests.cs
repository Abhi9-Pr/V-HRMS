using FluentValidation.TestHelper;
using Vespera.Application.Features.RotationPatterns;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class CreateRotationPatternCommandValidatorTests
{
    private readonly CreateRotationPatternCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateRotationPatternCommand(string.Empty, [new RotationPatternDayRequest(0, null)], null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Days_Is_Empty()
    {
        var command = new CreateRotationPatternCommand("Empty", [], null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Days);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateRotationPatternCommand("Pattern", [new RotationPatternDayRequest(0, null)], null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
