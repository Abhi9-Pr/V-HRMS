using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 220 — Professional Tax, capped the same way as <see cref="ProvidentFundRule"/>
/// (cap on the computed contribution, matching the seeded ₹200/month figure).</summary>
public sealed class ProfessionalTaxRule : IPayrollComponentRule
{
    public int Order => 220;

    public bool AppliesTo(PayrollContext context) => context.FindStatutoryRule(StatutoryRuleType.ProfessionalTax) is not null;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context)
    {
        var rule = context.FindStatutoryRule(StatutoryRuleType.ProfessionalTax)!;
        var wageBase = PayrollWageBase.AfterLossOfPay(context);
        var contribution = Money.Of(Math.Round(wageBase.Amount * rule.RatePercent / 100m, 2), context.Currency);
        if (rule.CapAmount is not null && contribution > rule.CapAmount)
        {
            contribution = rule.CapAmount;
        }

        return
        [
            new PayrollComponentLine(
                SyntheticSalaryComponentIds.ProfessionalTax, "Professional Tax", SalaryComponentType.StatutoryContribution,
                PayrollComponentDirection.Deduction, contribution),
        ];
    }
}
