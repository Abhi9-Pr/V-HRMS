using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class WithdrawLeaveRequestCommandValidatorTests
{
    private readonly WithdrawLeaveRequestCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LeaveRequestId_Is_Empty()
    {
        var result = _validator.TestValidate(new WithdrawLeaveRequestCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.LeaveRequestId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new WithdrawLeaveRequestCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
