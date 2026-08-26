using Vespera.Domain.Payroll;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>Order 500 — relays already-approved reimbursement claims as earning lines. Runs after
/// every deduction stage: reimbursements are not part of the taxable/statutory wage base those
/// stages compute from (<c>SumEarnings() - SumDeductions()</c> at their point in the pipeline
/// excludes these, since they haven't been appended yet).</summary>
public sealed class ReimbursementsRule : IPayrollComponentRule
{
    public int Order => 500;

    public bool AppliesTo(PayrollContext context) => context.Reimbursements.Count > 0;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context) =>
        [.. context.Reimbursements.Select(line => new PayrollComponentLine(
            line.ComponentId, line.ComponentName, SalaryComponentType.Reimbursement, PayrollComponentDirection.Earning, line.Amount))];
}
