using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class PayrollRunsPagedSpecification : ISpecification<PayrollRun>
{
    public PayrollRunsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = run => run.TenantId == tenantId;
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<PayrollRun, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<PayrollRun, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<PayrollRun, object>> KeySelector, bool Descending)> OrderBy { get; } =
        [(run => run.Year, true), (run => run.Month, true)];

    public (int Skip, int Take)? Paging { get; }
}
