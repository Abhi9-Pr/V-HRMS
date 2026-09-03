using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class SubmitLeaveRequestCommandValidatorTests
{
    private readonly SubmitLeaveRequestCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LeaveTypeId_Is_Empty()
    {
        var command = new SubmitLeaveRequestCommand(Guid.Empty, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), "Vacation", false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LeaveTypeId);
    }

    [Fact]
    public void Should_Have_Error_When_To_Is_Before_From()
    {
        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 1), "Vacation", false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.To);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), string.Empty, false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Exceeds_MaximumLength()
    {
        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), new string('a', 1001), false);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), "Vacation", false);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
