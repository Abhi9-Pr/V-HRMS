using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

/// <summary>The geofence "assigned" to a location — assumes at most one active zone per location
/// (a location with none configured means punches there are unrestricted).</summary>
public sealed class GeofenceZoneByLocationIdSpecification : ISpecification<GeofenceZone>
{
    public GeofenceZoneByLocationIdSpecification(TenantId tenantId, LocationId locationId)
    {
        Criteria = zone => zone.TenantId == tenantId && zone.LocationId == locationId && !zone.IsDeleted;
    }

    public Expression<Func<GeofenceZone, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<GeofenceZone, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<GeofenceZone, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
