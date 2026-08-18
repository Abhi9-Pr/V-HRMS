using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Attendance;

public readonly record struct GeofenceZoneId(Guid Value)
{
    public static GeofenceZoneId New() => new(Guid.NewGuid());
}

public sealed class GeofenceZone : AuditableTenantAggregateRoot<GeofenceZoneId>
{
    private GeofenceZone(
        GeofenceZoneId id, TenantId tenantId, string name, GeoCoordinate center, double radiusMetres,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        Center = center;
        RadiusMetres = radiusMetres;
    }

    public string Name { get; private set; }

    public GeoCoordinate Center { get; private set; }

    public double RadiusMetres { get; private set; }

    public static Result<GeofenceZone> Create(
        TenantId tenantId, string name, GeoCoordinate center, double radiusMetres, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<GeofenceZone>(Error.Validation("geofence_zone.name_required", "Geofence zone name is required."));
        }

        if (radiusMetres <= 0)
        {
            return Result.Failure<GeofenceZone>(Error.Validation("geofence_zone.invalid_radius", "Radius must be positive."));
        }

        return Result.Success(new GeofenceZone(GeofenceZoneId.New(), tenantId, name.Trim(), center, radiusMetres, occurredOn, createdBy));
    }

    public Result Resize(double radiusMetres, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (radiusMetres <= 0)
        {
            return Result.Failure(Error.Validation("geofence_zone.invalid_radius", "Radius must be positive."));
        }

        RadiusMetres = radiusMetres;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Relocate(GeoCoordinate center, DateTimeOffset occurredOn, string modifiedBy)
    {
        Center = center;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
