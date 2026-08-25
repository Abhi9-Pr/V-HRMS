using FluentValidation.TestHelper;
using Vespera.Application.Features.Shifts;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class CreateShiftCommandValidatorTests
{
    private readonly CreateShiftCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateShiftCommand(string.Empty, new TimeOnly(9, 0), new TimeOnly(18, 0), 10, 0, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_GraceMinutes_Is_Negative()
    {
        var command = new CreateShiftCommand("Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), -1, 0, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.GraceMinutes);
    }

    [Fact]
    public void Should_Have_Error_When_BreakMinutes_Is_Negative()
    {
        var command = new CreateShiftCommand("Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 0, -1, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BreakMinutes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateShiftCommand("Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, 30, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
