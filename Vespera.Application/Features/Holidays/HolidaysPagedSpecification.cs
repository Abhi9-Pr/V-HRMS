using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Holidays;

public sealed class HolidaysPagedSpecification : ISpecification<Holiday>
{
    public HolidaysPagedSpecification(TenantId tenantId, PagedRequest paging, Guid? locationId)
    {
        Criteria = locationId is { } location
            ? holiday => holiday.TenantId == tenantId && !holiday.IsDeleted && holiday.LocationId == new LocationId(location)
            : holiday => holiday.TenantId == tenantId && !holiday.IsDeleted;
        OrderBy = [(holiday => (object)holiday.Date, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Holiday, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Holiday, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Holiday, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
