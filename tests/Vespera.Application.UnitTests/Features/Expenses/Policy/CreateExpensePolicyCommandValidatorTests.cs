using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses.Policy;

public class CreateExpensePolicyCommandValidatorTests
{
    private readonly CreateExpensePolicyCommandValidator _validator = new();

    private static CreateExpensePolicyCommand ValidCommand() => new(
        "Travel", 5000m, 500m, Currency.Inr, null, ExpensePolicySeverity.Block, ExpensePolicySeverity.Warn, null);

    [Fact]
    public void Should_Have_Error_When_Category_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { Category = string.Empty });

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Should_Have_Error_When_Category_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(ValidCommand() with { Category = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Should_Have_Error_When_MaxAmountPerClaim_Is_Negative()
    {
        var result = _validator.TestValidate(ValidCommand() with { MaxAmountPerClaim = -1m });

        result.ShouldHaveValidationErrorFor(c => c.MaxAmountPerClaim);
    }

    [Fact]
    public void Should_Have_Error_When_ReceiptRequiredAboveAmount_Is_Negative()
    {
        var result = _validator.TestValidate(ValidCommand() with { ReceiptRequiredAboveAmount = -1m });

        result.ShouldHaveValidationErrorFor(c => c.ReceiptRequiredAboveAmount);
    }

    [Fact]
    public void Should_Have_Error_When_Currency_Is_Not_A_Defined_Enum_Value()
    {
        var result = _validator.TestValidate(ValidCommand() with { Currency = (Currency)999 });

        result.ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public void Should_Have_Error_When_MaxAmountSeverity_Is_Not_A_Defined_Enum_Value()
    {
        var result = _validator.TestValidate(ValidCommand() with { MaxAmountSeverity = (ExpensePolicySeverity)999 });

        result.ShouldHaveValidationErrorFor(c => c.MaxAmountSeverity);
    }

    [Fact]
    public void Should_Have_Error_When_ReceiptRequiredSeverity_Is_Not_A_Defined_Enum_Value()
    {
        var result = _validator.TestValidate(ValidCommand() with { ReceiptRequiredSeverity = (ExpensePolicySeverity)999 });

        result.ShouldHaveValidationErrorFor(c => c.ReceiptRequiredSeverity);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
