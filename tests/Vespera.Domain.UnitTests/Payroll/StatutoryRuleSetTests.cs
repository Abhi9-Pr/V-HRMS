using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class StatutoryRuleSetTests
{
    private static readonly TenantId TenantId = TenantId.New();

    [Fact]
    public void Create_Should_Succeed_With_A_Valid_Rate()
    {
        var result = StatutoryRuleSet.Create(
            TenantId, StatutoryRuleType.ProvidentFund, 12m, Money.Of(1800m, Currency.Inr), new DateOnly(2026, 1, 1), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.RuleType.Should().Be(StatutoryRuleType.ProvidentFund);
        result.Value.RatePercent.Should().Be(12m);
        result.Value.CapAmount!.Amount.Should().Be(1800m);
        result.Value.IsOpenEnded.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Create_Should_Fail_When_RatePercent_Is_Out_Of_Range(decimal ratePercent)
    {
        var result = StatutoryRuleSet.Create(TenantId, StatutoryRuleType.ProvidentFund, ratePercent, null, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("statutory_rule_set.invalid_rate");
    }

    [Fact]
    public void EndOn_Should_Close_The_Rule_When_ValidTo_Is_Not_Before_ValidFrom()
    {
        var rule = StatutoryRuleSet.Create(
            TenantId, StatutoryRuleType.EmployeeStateInsurance, 3.25m, null, new DateOnly(2026, 1, 1), null).Value;

        var result = rule.EndOn(new DateOnly(2026, 12, 31));

        result.IsSuccess.Should().BeTrue();
        rule.ValidTo.Should().Be(new DateOnly(2026, 12, 31));
        rule.IsOpenEnded.Should().BeFalse();
    }

    [Fact]
    public void EndOn_Should_Fail_When_ValidTo_Is_Before_ValidFrom()
    {
        var rule = StatutoryRuleSet.Create(
            TenantId, StatutoryRuleType.ProfessionalTax, 2m, null, new DateOnly(2026, 6, 1), null).Value;

        var result = rule.EndOn(new DateOnly(2026, 1, 1));

        result.IsFailure.Should().BeTrue();
        rule.IsOpenEnded.Should().BeTrue();
    }

    [Fact]
    public void IsActiveOn_Should_Respect_The_ValidFrom_And_ValidTo_Bounds()
    {
        var rule = StatutoryRuleSet.Create(
            TenantId, StatutoryRuleType.Gratuity, 4.81m, null, new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)).Value;

        rule.IsActiveOn(new DateOnly(2026, 3, 31)).Should().BeFalse();
        rule.IsActiveOn(new DateOnly(2026, 5, 1)).Should().BeTrue();
        rule.IsActiveOn(new DateOnly(2026, 7, 1)).Should().BeFalse();
    }

    [Fact]
    public void StatutoryRuleSetId_New_Should_Produce_Distinct_Non_Empty_Ids()
    {
        var first = StatutoryRuleSetId.New();
        var second = StatutoryRuleSetId.New();

        first.Value.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
    }
}
