using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class ClearPunchFlagCommandValidatorTests
{
    private readonly ClearPunchFlagCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var command = new ClearPunchFlagCommand(Guid.Empty, new DateOnly(2026, 1, 15), Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Have_Error_When_PunchId_Is_Empty()
    {
        var command = new ClearPunchFlagCommand(Guid.NewGuid(), new DateOnly(2026, 1, 15), Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.PunchId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new ClearPunchFlagCommand(Guid.NewGuid(), new DateOnly(2026, 1, 15), Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
