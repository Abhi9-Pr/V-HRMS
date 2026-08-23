using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Leave;

public sealed class HolidaysByLocationAndRangeSpecification : ISpecification<Holiday>
{
    public HolidaysByLocationAndRangeSpecification(TenantId tenantId, LocationId locationId, DateOnly from, DateOnly to)
    {
        Criteria = h => h.TenantId == tenantId && h.LocationId == locationId && h.Date >= from && h.Date <= to;
    }

    public Expression<Func<Holiday, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Holiday, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Holiday, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
