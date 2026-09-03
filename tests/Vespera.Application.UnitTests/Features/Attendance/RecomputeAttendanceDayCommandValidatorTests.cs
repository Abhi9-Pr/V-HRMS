using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class RecomputeAttendanceDayCommandValidatorTests
{
    private readonly RecomputeAttendanceDayCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var command = new RecomputeAttendanceDayCommand(Guid.Empty, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Have_Error_When_RangeEnd_Is_Before_RangeStart()
    {
        var command = new RecomputeAttendanceDayCommand(Guid.NewGuid(), new DateOnly(2026, 1, 7), new DateOnly(2026, 1, 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RangeEnd);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new RecomputeAttendanceDayCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
