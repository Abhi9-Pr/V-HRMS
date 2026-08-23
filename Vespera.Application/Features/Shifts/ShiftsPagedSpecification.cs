using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class ShiftsPagedSpecification : ISpecification<Shift>
{
    public ShiftsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = shift => shift.TenantId == tenantId && !shift.IsDeleted;
        OrderBy = [(shift => (object)shift.Name, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Shift, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Shift, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Shift, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
