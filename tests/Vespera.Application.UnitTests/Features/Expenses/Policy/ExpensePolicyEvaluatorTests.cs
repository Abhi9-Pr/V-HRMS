using FluentAssertions;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses.Policy;

public class ExpensePolicyEvaluatorTests
{
    [Fact]
    public void Evaluate_Should_Run_Every_Rule_Against_Every_Policy()
    {
        var evaluator = new ExpensePolicyEvaluator([new MaxAmountPerClaimRule(), new ReceiptRequiredAboveAmountRule()]);
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(5000m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        var policy = ExpensePolicy.Create(
            TenantId.New(), "Travel", Money.Of(1000m, Currency.Inr), Money.Of(500m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        var violations = evaluator.Evaluate(claim, [policy]);

        violations.Should().HaveCount(2);
        violations.Select(v => v.RuleCode).Should().Contain(["max_amount_per_claim", "receipt_required_above_amount"]);
    }

    [Fact]
    public void Evaluate_Should_Return_Nothing_When_There_Are_No_Applicable_Policies()
    {
        var evaluator = new ExpensePolicyEvaluator([new MaxAmountPerClaimRule(), new ReceiptRequiredAboveAmountRule()]);
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(5000m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);

        var violations = evaluator.Evaluate(claim, []);

        violations.Should().BeEmpty();
    }
}
