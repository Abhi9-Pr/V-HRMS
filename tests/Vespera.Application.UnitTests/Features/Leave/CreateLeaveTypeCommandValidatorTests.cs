using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CreateLeaveTypeCommandValidatorTests
{
    private readonly CreateLeaveTypeCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var result = _validator.TestValidate(new CreateLeaveTypeCommand(string.Empty, true, 5));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaximumLength()
    {
        var result = _validator.TestValidate(new CreateLeaveTypeCommand(new string('a', 129), true, 5));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_CarryForwardLimit_Is_Negative()
    {
        var result = _validator.TestValidate(new CreateLeaveTypeCommand("Sick Leave", true, -1));

        result.ShouldHaveValidationErrorFor(c => c.CarryForwardLimit);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new CreateLeaveTypeCommand("Sick Leave", true, 5));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
