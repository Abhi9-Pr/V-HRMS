using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class UpdateLeaveTypeEligibilityCommandValidatorTests
{
    private readonly UpdateLeaveTypeEligibilityCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LeaveTypeId_Is_Empty()
    {
        var result = _validator.TestValidate(new UpdateLeaveTypeEligibilityCommand(Guid.Empty, null, 0, false, 0));

        result.ShouldHaveValidationErrorFor(c => c.LeaveTypeId);
    }

    [Fact]
    public void Should_Have_Error_When_MinimumTenureMonths_Is_Negative()
    {
        var result = _validator.TestValidate(new UpdateLeaveTypeEligibilityCommand(Guid.NewGuid(), null, -1, false, 0));

        result.ShouldHaveValidationErrorFor(c => c.MinimumTenureMonths);
    }

    [Fact]
    public void Should_Have_Error_When_MaxEncashableDays_Is_Negative()
    {
        var result = _validator.TestValidate(new UpdateLeaveTypeEligibilityCommand(Guid.NewGuid(), null, 0, true, -1));

        result.ShouldHaveValidationErrorFor(c => c.MaxEncashableDays);
    }

    [Fact]
    public void Should_Have_Error_When_ApplicableGender_Is_Not_A_Known_Value()
    {
        var result = _validator.TestValidate(new UpdateLeaveTypeEligibilityCommand(Guid.NewGuid(), "NotAGender", 0, false, 0));

        result.ShouldHaveValidationErrorFor(c => c.ApplicableGender);
    }

    [Fact]
    public void Should_Not_Have_Error_When_ApplicableGender_Is_Null()
    {
        var result = _validator.TestValidate(new UpdateLeaveTypeEligibilityCommand(Guid.NewGuid(), null, 0, false, 0));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid_With_A_Known_Gender()
    {
        var result = _validator.TestValidate(new UpdateLeaveTypeEligibilityCommand(Guid.NewGuid(), "Female", 6, true, 5));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
