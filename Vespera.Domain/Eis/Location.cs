using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Eis;

public readonly record struct LocationId(Guid Value)
{
    public static LocationId New() => new(Guid.NewGuid());
}

public sealed class Location : AuditableTenantAggregateRoot<LocationId>
{
    private Location(
        LocationId id, TenantId tenantId, string name, string addressLine, string city, string country,
        GeoCoordinate coordinate, string timeZoneId, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        AddressLine = addressLine;
        City = city;
        Country = country;
        Coordinate = coordinate;
        TimeZoneId = timeZoneId;
    }

    public string Name { get; private set; }

    public string AddressLine { get; private set; }

    public string City { get; private set; }

    public string Country { get; private set; }

    public GeoCoordinate Coordinate { get; private set; }

    public string TimeZoneId { get; private set; }

    public static Result<Location> Create(
        TenantId tenantId, string name, string addressLine, string city, string country,
        GeoCoordinate coordinate, string timeZoneId, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Location>(Error.Validation("location.name_required", "Location name is required."));
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return Result.Failure<Location>(Error.Validation("location.timezone_required", "Time zone id is required."));
        }

        return Result.Success(new Location(
            LocationId.New(), tenantId, name.Trim(), addressLine, city, country, coordinate, timeZoneId, occurredOn, createdBy));
    }

    public Result Relocate(GeoCoordinate coordinate, string addressLine, string city, string country, DateTimeOffset occurredOn, string modifiedBy)
    {
        Coordinate = coordinate;
        AddressLine = addressLine;
        City = city;
        Country = country;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
