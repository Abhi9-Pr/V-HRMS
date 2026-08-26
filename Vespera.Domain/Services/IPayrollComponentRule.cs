using Vespera.Domain.Payroll;

namespace Vespera.Domain.Services;

/// <summary>
/// One stage of the payroll rules pipeline — Earnings, Attendance/LOP, Statutory (PF/ESI/PT),
/// Income Tax, Voluntary Deductions, Reimbursements, Net Pay. Each stage is its own class,
/// registered in DI and selected at runtime by <see cref="Order"/> and <see cref="AppliesTo"/> —
/// adding a new component (a new statutory levy, a new deduction type) means writing one new
/// class and one registration line, never editing an existing rule. Deliberately synchronous and
/// side-effect free: all I/O (loading the salary structure, statutory rates, tax regime,
/// declaration) happens before a rule runs, in whatever builds the <see cref="PayrollContext"/> —
/// keeping Domain free of external dependencies per AGENTS.md. The orchestrator resolves every
/// registered rule, sorts by <see cref="Order"/>, and for each one calls <see cref="AppliesTo"/>
/// before <see cref="Apply"/> — an inapplicable rule (e.g. ESI above the wage ceiling) is skipped
/// rather than emitting a zero line, which is what lets a variance report and a golden-file
/// snapshot stay meaningful.
/// </summary>
public interface IPayrollComponentRule
{
    /// <summary>Pipeline position. Stages are spaced in hundreds (Earnings 0, Attendance/LOP 100,
    /// Statutory 200-299, Income Tax 300, Voluntary Deductions 400, Reimbursements 500, Net Pay
    /// 600) so a future stage can be inserted without renumbering the rest.</summary>
    public int Order { get; }

    /// <summary>Whether this rule has anything to contribute for this employee this period — e.g.
    /// an employee above the ESI wage ceiling should produce no ESI line at all, not a zero one.</summary>
    public bool AppliesTo(PayrollContext context);

    /// <summary>Computes this stage's line items from <paramref name="context"/>, including
    /// whatever earlier stages already appended to <see cref="PayrollContext.Lines"/>. Must be a
    /// pure function of <paramref name="context"/> — same input, same output, every time — since
    /// that determinism is what makes two dry-runs on frozen inputs byte-identical.</summary>
    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context);
}
