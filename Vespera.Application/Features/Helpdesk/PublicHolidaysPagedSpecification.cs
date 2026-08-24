using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed class PublicHolidaysPagedSpecification : ISpecification<PublicHoliday>
{
    public PublicHolidaysPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = holiday => holiday.TenantId == tenantId;
        OrderBy = [(holiday => (object)holiday.Date, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<PublicHoliday, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<PublicHoliday, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<PublicHoliday, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
