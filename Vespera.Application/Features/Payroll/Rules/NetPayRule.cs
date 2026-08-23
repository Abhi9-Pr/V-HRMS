using Vespera.Domain.Payroll;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>
/// Order 600 — the terminal stage. Net pay itself isn't a "component" in its own right; it's the
/// rollup <see cref="PayrollComputationEngine"/> computes from every earlier stage's output
/// (<c>SumEarnings() - SumDeductions()</c>), so this rule contributes no lines. It's still
/// registered so the pipeline's staged design is complete end to end in the DI-registration
/// architecture test, and so a real negative-net-pay policy has an obvious place to grow into later
/// without renumbering the rest of the pipeline — negative-net-pay detection today lives in the
/// variance report, which can return a proper <c>Result</c> failure; this rule's <c>Apply</c>
/// contract cannot.
/// </summary>
public sealed class NetPayRule : IPayrollComponentRule
{
    public int Order => 600;

    public bool AppliesTo(PayrollContext context) => true;

    public IReadOnlyList<PayrollComponentLine> Apply(PayrollContext context) => [];
}
