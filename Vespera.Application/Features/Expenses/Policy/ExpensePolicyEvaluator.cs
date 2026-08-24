using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class ExpensePolicyEvaluator
{
    private readonly IReadOnlyList<IExpensePolicyRule> _rules;

    public ExpensePolicyEvaluator(IEnumerable<IExpensePolicyRule> rules)
    {
        _rules = rules.OrderBy(rule => rule.Order).ToList();
    }

    public IReadOnlyList<PolicyViolation> Evaluate(ExpenseClaim claim, IReadOnlyList<ExpensePolicy> policies)
    {
        var violations = new List<PolicyViolation>();

        foreach (var policy in policies)
        {
            foreach (var rule in _rules)
            {
                violations.AddRange(rule.Evaluate(claim, policy));
            }
        }

        return violations;
    }
}
