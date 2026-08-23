using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

/// <summary>Whether a specific location observes a holiday on a specific date — used by
/// attendance-day computation, distinct from <c>Holidays</c>' own paged/admin specifications.</summary>
public sealed class HolidayByLocationAndDateSpecification : ISpecification<Holiday>
{
    public HolidayByLocationAndDateSpecification(TenantId tenantId, LocationId locationId, DateOnly date)
    {
        Criteria = holiday =>
            holiday.TenantId == tenantId && holiday.LocationId == locationId && holiday.Date == date && !holiday.IsDeleted;
    }

    public Expression<Func<Holiday, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Holiday, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Holiday, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
