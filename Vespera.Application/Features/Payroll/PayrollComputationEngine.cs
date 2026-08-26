using Vespera.Domain.Payroll;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Payroll;

/// <summary>
/// Orchestrates the payroll rules pipeline: resolves every registered <see cref="IPayrollComponentRule"/>,
/// sorts by <see cref="IPayrollComponentRule.Order"/> — not registration order, which is what makes
/// two runs on the same frozen inputs produce identical output — and for one employee's
/// already-built <see cref="PayrollContext"/>, runs each applicable rule in turn, letting each
/// stage read every earlier stage's output via <see cref="PayrollContext.Lines"/>. Building the
/// context itself (loading the salary structure, statutory rules, tax regime, investment
/// declaration, LOP days) is the caller's job — a command handler with repository access, not this
/// class — so this engine stays testable without needing five stubbed repositories.
/// </summary>
public sealed class PayrollComputationEngine
{
    private readonly IReadOnlyList<IPayrollComponentRule> _rules;

    public PayrollComputationEngine(IEnumerable<IPayrollComponentRule> rules)
    {
        _rules = [.. rules.OrderBy(rule => rule.Order)];
    }

    public PayrollLineInput ComputeForEmployee(PayrollContext context)
    {
        foreach (var rule in _rules)
        {
            if (rule.AppliesTo(context))
            {
                context.AppendLines(rule.Apply(context));
            }
        }

        var gross = context.SumEarnings();
        var deductions = context.SumDeductions();
        var net = gross - deductions;

        return new PayrollLineInput(context.EmployeeId, gross, deductions, net, context.LossOfPayDays);
    }
}
