using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class UpdateLeavePolicySettingsCommandValidatorTests
{
    private readonly UpdateLeavePolicySettingsCommandValidator _validator = new();

    private static UpdateLeavePolicySettingsCommand ValidCommand() => new(
        Guid.NewGuid(), "Monthly", 0, false, null, false, "NotAllowed", 0, false);

    [Fact]
    public void Should_Have_Error_When_LeavePolicyId_Is_Empty()
    {
        var command = ValidCommand() with { LeavePolicyId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LeavePolicyId);
    }

    [Fact]
    public void Should_Have_Error_When_MinimumTenureMonthsForAccrual_Is_Negative()
    {
        var command = ValidCommand() with { MinimumTenureMonthsForAccrual = -1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.MinimumTenureMonthsForAccrual);
    }

    [Fact]
    public void Should_Have_Error_When_MaxNegativeBalanceDays_Is_Negative()
    {
        var command = ValidCommand() with { MaxNegativeBalanceDays = -1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.MaxNegativeBalanceDays);
    }

    [Fact]
    public void Should_Have_Error_When_AccrualFrequency_Is_Not_A_Known_Value()
    {
        var command = ValidCommand() with { AccrualFrequency = "Weekly" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AccrualFrequency);
    }

    [Fact]
    public void Should_Have_Error_When_NegativeBalancePolicy_Is_Not_A_Known_Value()
    {
        var command = ValidCommand() with { NegativeBalancePolicy = "Unlimited" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.NegativeBalancePolicy);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
