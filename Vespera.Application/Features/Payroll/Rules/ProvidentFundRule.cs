using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 200 — employee PF contribution: <c>RatePercent</c> of the LOP-adjusted wage,
/// capped at the effective <see cref="StatutoryRuleSet.CapAmount"/> (the real-world EPF wage-ceiling
/// contribution cap, e.g. 12% of a ₹15,000 ceiling = ₹1,800 — modelled here directly as a cap on
/// the computed contribution, matching how <c>DevelopmentSeeder</c> seeds it).</summary>
public sealed class ProvidentFundRule : IPayrollComponentRule
{
    public int Order => 200;

    public bool AppliesTo(PayrollContext context) => context.FindStatutoryRule(StatutoryRuleType.ProvidentFund) is not null;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context)
    {
        var rule = context.FindStatutoryRule(StatutoryRuleType.ProvidentFund)!;
        var wageBase = PayrollWageBase.AfterLossOfPay(context);
        var contribution = Money.Of(Math.Round(wageBase.Amount * rule.RatePercent / 100m, 2), context.Currency);
        if (rule.CapAmount is not null && contribution > rule.CapAmount)
        {
            contribution = rule.CapAmount;
        }

        return
        [
            new PayrollComponentLine(
                SyntheticSalaryComponentIds.ProvidentFund, "Provident Fund", SalaryComponentType.StatutoryContribution,
                PayrollComponentDirection.Deduction, contribution),
        ];
    }
}
