using Vespera.Domain.Payroll;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 100 — deducts unpaid leave days at a per-day rate derived from the Earnings
/// stage's output (gross-for-period / days-in-period). Runs before the statutory stages so PF/ESI/PT
/// are computed on the LOP-adjusted wage, matching how those contributions actually work.</summary>
public sealed class AttendanceLopRule : IPayrollComponentRule
{
    public int Order => 100;

    public bool AppliesTo(PayrollContext context) => context.LossOfPayDays > 0;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context)
    {
        var deduction = context.SumEarnings() - PayrollWageBase.AfterLossOfPay(context);

        return
        [
            new PayrollComponentLine(
                SyntheticSalaryComponentIds.LossOfPay, "Loss of Pay", SalaryComponentType.Deduction,
                PayrollComponentDirection.Deduction, deduction),
        ];
    }
}
