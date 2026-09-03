using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance.Regularizations;

namespace Vespera.Application.UnitTests.Features.Attendance.Regularizations;

public class SubmitRegularizationCommandValidatorTests
{
    private readonly SubmitRegularizationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AttendanceDayId_Is_Empty()
    {
        var command = new SubmitRegularizationCommand(Guid.Empty, "Forgot to punch out", null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AttendanceDayId);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var command = new SubmitRegularizationCommand(Guid.NewGuid(), string.Empty, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Exceeds_Max_Length()
    {
        var command = new SubmitRegularizationCommand(Guid.NewGuid(), new string('a', 1001), null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Should_Have_Error_When_Evidence_Content_Is_Provided_Without_A_File_Name()
    {
        var command = new SubmitRegularizationCommand(Guid.NewGuid(), "Forgot to punch out", null, [1, 2, 3]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EvidenceFileName);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid_Without_Evidence()
    {
        var command = new SubmitRegularizationCommand(Guid.NewGuid(), "Forgot to punch out", null, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid_With_Evidence()
    {
        var command = new SubmitRegularizationCommand(Guid.NewGuid(), "Forgot to punch out", "receipt.png", [1, 2, 3]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
