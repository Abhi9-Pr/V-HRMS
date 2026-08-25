using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Holidays;

/// <summary>Every non-deleted holiday for the tenant, optionally scoped to one location, unpaged —
/// used by <see cref="GetHolidaysETagQuery"/>'s tag computation.</summary>
public sealed class HolidaysByTenantAndLocationSpecification : ISpecification<Holiday>
{
    public HolidaysByTenantAndLocationSpecification(TenantId tenantId, LocationId? locationId)
    {
        Criteria = locationId is { } id
            ? holiday => holiday.TenantId == tenantId && !holiday.IsDeleted && holiday.LocationId == id
            : holiday => holiday.TenantId == tenantId && !holiday.IsDeleted;
    }

    public Expression<Func<Holiday, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Holiday, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Holiday, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
