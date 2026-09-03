using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class ApproveLeaveRequestCommandValidatorTests
{
    private readonly ApproveLeaveRequestCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LeaveRequestId_Is_Empty()
    {
        var result = _validator.TestValidate(new ApproveLeaveRequestCommand(Guid.Empty, "Looks good"));

        result.ShouldHaveValidationErrorFor(c => c.LeaveRequestId);
    }

    [Fact]
    public void Should_Have_Error_When_Comment_Exceeds_MaximumLength()
    {
        var result = _validator.TestValidate(new ApproveLeaveRequestCommand(Guid.NewGuid(), new string('a', 1001)));

        result.ShouldHaveValidationErrorFor(c => c.Comment);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new ApproveLeaveRequestCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
