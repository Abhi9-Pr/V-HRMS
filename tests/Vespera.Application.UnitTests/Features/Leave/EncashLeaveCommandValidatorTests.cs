using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class EncashLeaveCommandValidatorTests
{
    private readonly EncashLeaveCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LeaveTypeId_Is_Empty()
    {
        var result = _validator.TestValidate(new EncashLeaveCommand(Guid.Empty, 5));

        result.ShouldHaveValidationErrorFor(c => c.LeaveTypeId);
    }

    [Fact]
    public void Should_Have_Error_When_Days_Is_Not_Positive()
    {
        var result = _validator.TestValidate(new EncashLeaveCommand(Guid.NewGuid(), 0));

        result.ShouldHaveValidationErrorFor(c => c.Days);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new EncashLeaveCommand(Guid.NewGuid(), 5));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
