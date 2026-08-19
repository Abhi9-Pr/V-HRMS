using Vespera.Domain.Common;

namespace Vespera.Domain.ValueObjects;

public sealed class GeoCoordinate : ValueObject
{
    private GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    // Latitude/longitude are inherently continuous measurements, not currency — double is
    // appropriate here (AGENTS.md bans double/float for Money specifically).
    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<GeoCoordinate> Create(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            return Result.Failure<GeoCoordinate>(
                Error.Validation("geo_coordinate.invalid_latitude", "Latitude must be between -90 and 90."));
        }

        if (longitude is < -180 or > 180)
        {
            return Result.Failure<GeoCoordinate>(
                Error.Validation("geo_coordinate.invalid_longitude", "Longitude must be between -180 and 180."));
        }

        return Result.Success(new GeoCoordinate(latitude, longitude));
    }

    public override string ToString() => $"({Latitude}, {Longitude})";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
