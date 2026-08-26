using FluentAssertions;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses.Policy;

public class ReceiptRequiredAboveAmountRuleTests
{
    private readonly ReceiptRequiredAboveAmountRule _rule = new();

    [Fact]
    public void Evaluate_Should_Flag_A_Line_Above_The_Threshold_With_No_Receipt()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(5000m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(100000m, Currency.Inr), Money.Of(1000m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        var violations = _rule.Evaluate(claim, policy);

        violations.Should().ContainSingle();
    }

    [Fact]
    public void Evaluate_Should_Not_Flag_A_Line_With_A_Receipt()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(5000m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: "receipt-1");
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(100000m, Currency.Inr), Money.Of(1000m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        var violations = _rule.Evaluate(claim, policy);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_Should_Ignore_Lines_In_A_Different_Category()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine("Meals", Money.Of(5000m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(100000m, Currency.Inr), Money.Of(1000m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        var violations = _rule.Evaluate(claim, policy);

        violations.Should().BeEmpty();
    }
}
