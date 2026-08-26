using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class ReceiptRequiredAboveAmountRule : IExpensePolicyRule
{
    public int Order => 2;

    public IReadOnlyList<PolicyViolation> Evaluate(ExpenseClaim claim, ExpensePolicy policy)
    {
        var violations = new List<PolicyViolation>();

        foreach (var line in claim.Lines)
        {
            if (!string.Equals(line.Category, policy.Category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Amount.Currency != policy.ReceiptRequiredAboveAmount.Currency)
            {
                continue;
            }

            if (line.Amount <= policy.ReceiptRequiredAboveAmount)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line.ReceiptReference))
            {
                continue;
            }

            violations.Add(new PolicyViolation(
                "receipt_required_above_amount",
                policy.ReceiptRequiredSeverity,
                $"A receipt is required for the '{line.Category}' line of {line.Amount} " +
                $"(policy threshold {policy.ReceiptRequiredAboveAmount})."));
        }

        return violations;
    }
}
