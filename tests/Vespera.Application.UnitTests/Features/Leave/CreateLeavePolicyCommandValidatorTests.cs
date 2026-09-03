using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CreateLeavePolicyCommandValidatorTests
{
    private readonly CreateLeavePolicyCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LeaveTypeId_Is_Empty()
    {
        var result = _validator.TestValidate(new CreateLeavePolicyCommand(Guid.Empty, 12, 1, 5, new DateOnly(2026, 1, 1)));

        result.ShouldHaveValidationErrorFor(c => c.LeaveTypeId);
    }

    [Fact]
    public void Should_Have_Error_When_AnnualEntitlementDays_Is_Negative()
    {
        var result = _validator.TestValidate(new CreateLeavePolicyCommand(Guid.NewGuid(), -1, 1, 5, new DateOnly(2026, 1, 1)));

        result.ShouldHaveValidationErrorFor(c => c.AnnualEntitlementDays);
    }

    [Fact]
    public void Should_Have_Error_When_AccrualRatePerMonth_Is_Negative()
    {
        var result = _validator.TestValidate(new CreateLeavePolicyCommand(Guid.NewGuid(), 12, -1, 5, new DateOnly(2026, 1, 1)));

        result.ShouldHaveValidationErrorFor(c => c.AccrualRatePerMonth);
    }

    [Fact]
    public void Should_Have_Error_When_MaxCarryForwardDays_Is_Negative()
    {
        var result = _validator.TestValidate(new CreateLeavePolicyCommand(Guid.NewGuid(), 12, 1, -1, new DateOnly(2026, 1, 1)));

        result.ShouldHaveValidationErrorFor(c => c.MaxCarryForwardDays);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new CreateLeavePolicyCommand(Guid.NewGuid(), 12, 1, 5, new DateOnly(2026, 1, 1)));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
