using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance.Regularizations;

namespace Vespera.Application.UnitTests.Features.Attendance.Regularizations;

public class RejectRegularizationCommandValidatorTests
{
    private readonly RejectRegularizationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RequestId_Is_Empty()
    {
        var command = new RejectRegularizationCommand(Guid.Empty, "Insufficient evidence");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RequestId);
    }

    [Fact]
    public void Should_Have_Error_When_RejectionReason_Is_Empty()
    {
        var command = new RejectRegularizationCommand(Guid.NewGuid(), string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RejectionReason);
    }

    [Fact]
    public void Should_Have_Error_When_RejectionReason_Exceeds_Max_Length()
    {
        var command = new RejectRegularizationCommand(Guid.NewGuid(), new string('a', 1001));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RejectionReason);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new RejectRegularizationCommand(Guid.NewGuid(), "Insufficient evidence");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
