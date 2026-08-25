using FluentValidation.TestHelper;
using Vespera.Application.Features.Rosters;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class GenerateRosterCommandValidatorTests
{
    private readonly GenerateRosterCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RotationPatternId_Is_Empty()
    {
        var command = new GenerateRosterCommand(Guid.Empty, [Guid.NewGuid()], new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RotationPatternId);
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeIds_Is_Empty()
    {
        var command = new GenerateRosterCommand(Guid.NewGuid(), [], new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeIds);
    }

    [Fact]
    public void Should_Have_Error_When_RangeEnd_Before_RangeStart()
    {
        var command = new GenerateRosterCommand(
            Guid.NewGuid(), [Guid.NewGuid()], new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RangeEnd);
    }

    [Fact]
    public void Should_Have_Error_When_Range_Exceeds_Max_Days()
    {
        var start = new DateOnly(2026, 1, 1);
        var command = new GenerateRosterCommand(Guid.NewGuid(), [Guid.NewGuid()], start, start.AddDays(400), start);

        var result = _validator.TestValidate(command);

        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var start = new DateOnly(2026, 1, 1);
        var command = new GenerateRosterCommand(Guid.NewGuid(), [Guid.NewGuid()], start, start.AddDays(6), start);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
