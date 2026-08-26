using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 0 — the pipeline's first stage. Turns the employee's already-resolved salary
/// structure amounts (<see cref="PayrollContext.ResolvedSalaryComponents"/>, produced by
/// <c>SalaryStructureResolver</c> before the pipeline runs) into earning lines, pro-rating for a
/// mid-period joiner or exit.</summary>
public sealed class EarningsRule : IPayrollComponentRule
{
    public int Order => 0;

    public bool AppliesTo(PayrollContext context) => context.ResolvedSalaryComponents.Count > 0;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context)
    {
        var effectiveStart = context.DateOfJoining > context.PeriodStart ? context.DateOfJoining : context.PeriodStart;
        var effectiveEnd = context.ExitDate is { } exitDate && exitDate < context.PeriodEnd ? exitDate : context.PeriodEnd;
        var isFullPeriod = effectiveStart == context.PeriodStart && effectiveEnd == context.PeriodEnd;
        var effectiveDays = Math.Max(0, effectiveEnd.DayNumber - effectiveStart.DayNumber + 1);

        var lines = new List<PayrollComponentLine>();
        foreach (var (componentId, amount) in context.ResolvedSalaryComponents)
        {
            var component = context.SalaryComponents.FirstOrDefault(c => c.Id == componentId);
            if (component is null)
            {
                continue;
            }

            var proratedAmount = isFullPeriod
                ? amount
                : Money.Of(Math.Round(amount.Amount * effectiveDays / context.DaysInPeriod, 2), amount.Currency);

            lines.Add(new PayrollComponentLine(
                componentId, component.Name, component.ComponentType, PayrollComponentDirection.Earning, proratedAmount));
        }

        return lines;
    }
}
