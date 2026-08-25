using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class MaxAmountPerClaimRule : IExpensePolicyRule
{
    public int Order => 1;

    public IReadOnlyList<PolicyViolation> Evaluate(ExpenseClaim claim, ExpensePolicy policy)
    {
        if (claim.SettlementCurrency != policy.MaxAmountPerClaim.Currency)
        {
            return [];
        }

        var total = claim.Total(claim.SettlementCurrency);
        if (total <= policy.MaxAmountPerClaim)
        {
            return [];
        }

        return
        [
            new PolicyViolation(
                "max_amount_per_claim",
                policy.MaxAmountSeverity,
                $"Claim total {total} exceeds the policy cap of {policy.MaxAmountPerClaim} for category '{policy.Category}'."),
        ];
    }
}
