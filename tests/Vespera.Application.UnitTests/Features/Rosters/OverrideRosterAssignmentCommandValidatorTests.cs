using FluentValidation.TestHelper;
using Vespera.Application.Features.Rosters;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class OverrideRosterAssignmentCommandValidatorTests
{
    private readonly OverrideRosterAssignmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var command = new OverrideRosterAssignmentCommand(Guid.Empty, new DateOnly(2026, 1, 1), Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Have_Error_When_ShiftId_Is_Empty()
    {
        var command = new OverrideRosterAssignmentCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ShiftId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new OverrideRosterAssignmentCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
