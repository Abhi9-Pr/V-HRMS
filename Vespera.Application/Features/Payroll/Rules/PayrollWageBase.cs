using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>
/// The LOP-adjusted gross wage that <see cref="AttendanceLopRule"/>, <see cref="ProvidentFundRule"/>,
/// <see cref="EmployeeStateInsuranceRule"/>, <see cref="ProfessionalTaxRule"/>, and
/// <see cref="IncomeTaxRule"/> all compute against. Deliberately re-derived from
/// <see cref="PayrollContext.SumEarnings"/> and <see cref="PayrollContext.LossOfPayDays"/> directly
/// rather than <c>SumEarnings() - SumDeductions()</c>: PF, ESI, and PT are each computed
/// independently off the same base in real payroll, not cascading off each other's already-posted
/// deduction lines — using <c>SumDeductions()</c> would make PT's base shrink by whatever PF
/// already deducted purely because PF's <see cref="IPayrollComponentRule.Order"/> happens to run
/// first, which is not how any of these statutory rules actually work.
/// </summary>
public static class PayrollWageBase
{
    public static Money AfterLossOfPay(PayrollContext context)
    {
        var gross = context.SumEarnings();
        if (context.LossOfPayDays <= 0)
        {
            return gross;
        }

        var perDayRate = gross.Amount / context.DaysInPeriod;
        var lop = Math.Round(perDayRate * context.LossOfPayDays, 2);
        return Money.Of(gross.Amount - lop, gross.Currency);
    }
}
