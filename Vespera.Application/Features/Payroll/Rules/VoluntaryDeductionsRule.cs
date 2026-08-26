using Vespera.Domain.Payroll;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 400 — relays already-decided voluntary deduction elections (loan EMIs, welfare
/// fund, etc.) as component lines. The election amounts themselves are resolved by whatever builds
/// the <see cref="PayrollContext"/>, not by this rule.</summary>
public sealed class VoluntaryDeductionsRule : IPayrollComponentRule
{
    public int Order => 400;

    public bool AppliesTo(PayrollContext context) => context.VoluntaryDeductions.Count > 0;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context) =>
        [.. context.VoluntaryDeductions.Select(line => new PayrollComponentLine(
            line.ComponentId, line.ComponentName, SalaryComponentType.Deduction, PayrollComponentDirection.Deduction, line.Amount))];
}
