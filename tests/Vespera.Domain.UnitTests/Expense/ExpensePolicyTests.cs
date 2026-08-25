using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Expense;

public class ExpensePolicyTests
{
    [Fact]
    public void Create_Should_Fail_When_Category_Is_Blank()
    {
        var result = ExpensePolicy.Create(
            TenantId.New(), " ", Money.Of(5000m, Currency.Inr), Money.Of(1000m, Currency.Inr), DateTimeOffset.UtcNow, "system");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Default_Severities_To_Block_And_Apply_To_All_Designations()
    {
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(5000m, Currency.Inr), Money.Of(1000m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        policy.ApplicableDesignationId.Should().BeNull();
        policy.MaxAmountSeverity.Should().Be(ExpensePolicySeverity.Block);
        policy.ReceiptRequiredSeverity.Should().Be(ExpensePolicySeverity.Block);
    }

    [Fact]
    public void UpdateLimits_Should_Change_Limits_Severities_And_Target_Designation()
    {
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(5000m, Currency.Inr), Money.Of(1000m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;
        var designationId = DesignationId.New();

        var result = policy.UpdateLimits(
            Money.Of(8000m, Currency.Inr), Money.Of(2000m, Currency.Inr), designationId,
            ExpensePolicySeverity.Warn, ExpensePolicySeverity.Warn, DateTimeOffset.UtcNow, "system");

        result.IsSuccess.Should().BeTrue();
        policy.MaxAmountPerClaim.Should().Be(Money.Of(8000m, Currency.Inr));
        policy.ReceiptRequiredAboveAmount.Should().Be(Money.Of(2000m, Currency.Inr));
        policy.ApplicableDesignationId.Should().Be(designationId);
        policy.MaxAmountSeverity.Should().Be(ExpensePolicySeverity.Warn);
        policy.ReceiptRequiredSeverity.Should().Be(ExpensePolicySeverity.Warn);
    }
}
