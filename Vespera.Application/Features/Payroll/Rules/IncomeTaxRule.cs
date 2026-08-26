using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 300 — monthly TDS: annualizes the LOP-adjusted monthly wage, subtracts
/// <see cref="PayrollContext.ApprovedInvestmentExemptions"/> (finance-approved investment
/// declaration lines only — see <c>InvestmentDeclaration.ApprovedExemptionTotal</c>), runs the
/// result through the employee's chosen <see cref="TaxRegimeVersion.CalculateTax"/> (old or new
/// regime — reused unmodified), then divides by 12. Only applies if a regime has actually been
/// resolved for this employee/period.</summary>
public sealed class IncomeTaxRule : IPayrollComponentRule
{
    public int Order => 300;

    public bool AppliesTo(PayrollContext context) => context.TaxRegimeVersion is not null;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context)
    {
        var wageBase = PayrollWageBase.AfterLossOfPay(context);
        var annualizedIncome = Money.Of(wageBase.Amount * 12m, context.Currency);
        var taxableIncome = annualizedIncome - context.ApprovedInvestmentExemptions;
        if (taxableIncome.Amount < 0)
        {
            taxableIncome = Money.Zero(context.Currency);
        }

        var annualTax = context.TaxRegimeVersion!.CalculateTax(taxableIncome);
        var monthlyTds = Money.Of(Math.Round(annualTax.Amount / 12m, 2), context.Currency);

        if (monthlyTds.Amount <= 0)
        {
            return [];
        }

        return
        [
            new PayrollComponentLine(
                SyntheticSalaryComponentIds.IncomeTax, "Income Tax (TDS)", SalaryComponentType.Deduction,
                PayrollComponentDirection.Deduction, monthlyTds),
        ];
    }
}
