using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 210 — employee ESI contribution. Unlike <see cref="ProvidentFundRule"/>,
/// <see cref="StatutoryRuleSet.CapAmount"/> here is a wage-eligibility ceiling, not a contribution
/// cap: <see cref="AppliesTo"/> returns false once the LOP-adjusted wage exceeds it, which is the
/// real ESI rule ("no ESI at all above the wage ceiling") and the brief's example of
/// <c>AppliesTo</c> actually mattering rather than every rule always firing.</summary>
public sealed class EmployeeStateInsuranceRule : IPayrollComponentRule
{
    public int Order => 210;

    public bool AppliesTo(PayrollContext context)
    {
        var rule = context.FindStatutoryRule(StatutoryRuleType.EmployeeStateInsurance);
        if (rule is null)
        {
            return false;
        }

        var wageBase = PayrollWageBase.AfterLossOfPay(context);
        return rule.CapAmount is null || wageBase <= rule.CapAmount;
    }

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context)
    {
        var rule = context.FindStatutoryRule(StatutoryRuleType.EmployeeStateInsurance)!;
        var wageBase = PayrollWageBase.AfterLossOfPay(context);
        var contribution = Money.Of(Math.Round(wageBase.Amount * rule.RatePercent / 100m, 2), context.Currency);

        return
        [
            new PayrollComponentLine(
                SyntheticSalaryComponentIds.EmployeeStateInsurance, "Employee State Insurance", SalaryComponentType.StatutoryContribution,
                PayrollComponentDirection.Deduction, contribution),
        ];
    }
}
