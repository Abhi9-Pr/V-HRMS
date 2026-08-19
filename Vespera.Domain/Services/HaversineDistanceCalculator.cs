using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Services;

/// <summary>
/// Great-circle distance via the haversine formula. Kept behind IDistanceCalculator so a more
/// precise implementation (e.g. Vincenty) can be swapped in later without touching callers.
/// </summary>
public sealed class HaversineDistanceCalculator : IDistanceCalculator
{
    private const double EarthRadiusMetres = 6_371_000d;

    public double DistanceMetres(GeoCoordinate first, GeoCoordinate second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var lat1 = DegreesToRadians(first.Latitude);
        var lat2 = DegreesToRadians(second.Latitude);
        var deltaLat = DegreesToRadians(second.Latitude - first.Latitude);
        var deltaLon = DegreesToRadians(second.Longitude - first.Longitude);

        var a = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)) +
                (Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2));
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMetres * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
