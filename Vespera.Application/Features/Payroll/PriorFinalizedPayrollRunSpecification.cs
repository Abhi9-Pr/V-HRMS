using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>The most recent Finalized/Published run before <paramref name="excludingRunId"/> — what
/// <c>GetPayrollRunVarianceQuery</c> diffs the dry-run against. Ordered by year/month descending so
/// <c>FirstOrDefaultAsync</c> yields the closest prior cycle, not just any earlier one.</summary>
public sealed class PriorFinalizedPayrollRunSpecification : ISpecification<PayrollRun>
{
    public PriorFinalizedPayrollRunSpecification(TenantId tenantId, PayrollRunId excludingRunId)
    {
        Criteria = run =>
            run.TenantId == tenantId && run.Id != excludingRunId &&
            (run.Status == PayrollRunStatus.Finalized || run.Status == PayrollRunStatus.Published);
    }

    public Expression<Func<PayrollRun, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<PayrollRun, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<PayrollRun, object>> KeySelector, bool Descending)> OrderBy { get; } =
        [(run => run.Year, true), (run => run.Month, true)];

    public (int Skip, int Take)? Paging => null;
}
