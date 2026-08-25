using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed record PolicyViolation(string RuleCode, ExpensePolicySeverity Severity, string Message);

/// <summary>One evaluable rule of the category policy engine. OCP: add a new rule by adding a new
/// implementation and one DI registration line — never by editing an existing rule.</summary>
public interface IExpensePolicyRule
{
    public int Order { get; }

    public IReadOnlyList<PolicyViolation> Evaluate(ExpenseClaim claim, ExpensePolicy policy);
}
