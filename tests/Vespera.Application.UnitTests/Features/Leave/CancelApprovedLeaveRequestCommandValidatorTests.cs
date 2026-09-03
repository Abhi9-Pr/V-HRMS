using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CancelApprovedLeaveRequestCommandValidatorTests
{
    private readonly CancelApprovedLeaveRequestCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LeaveRequestId_Is_Empty()
    {
        var result = _validator.TestValidate(new CancelApprovedLeaveRequestCommand(Guid.Empty, "Plans changed"));

        result.ShouldHaveValidationErrorFor(c => c.LeaveRequestId);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var result = _validator.TestValidate(new CancelApprovedLeaveRequestCommand(Guid.NewGuid(), string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new CancelApprovedLeaveRequestCommand(Guid.NewGuid(), "Plans changed"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
