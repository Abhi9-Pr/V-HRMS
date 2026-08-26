using FluentAssertions;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses.Policy;

public class MaxAmountPerClaimRuleTests
{
    private readonly MaxAmountPerClaimRule _rule = new();

    [Fact]
    public void Evaluate_Should_Return_A_Violation_When_The_Claim_Total_Exceeds_The_Cap()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(5000m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(1000m, Currency.Inr), Money.Of(0m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        var violations = _rule.Evaluate(claim, policy);

        violations.Should().ContainSingle(v => v.Severity == ExpensePolicySeverity.Block);
    }

    [Fact]
    public void Evaluate_Should_Return_Nothing_When_Within_The_Cap()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(1000m, Currency.Inr), Money.Of(0m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        var violations = _rule.Evaluate(claim, policy);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_Should_Skip_When_Currencies_Differ()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Usd);
        claim.AddLine("Travel", Money.Of(5000m, Currency.Usd), new DateOnly(2026, 1, 10), null);
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(1000m, Currency.Inr), Money.Of(0m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        var violations = _rule.Evaluate(claim, policy);

        violations.Should().BeEmpty();
    }
}
