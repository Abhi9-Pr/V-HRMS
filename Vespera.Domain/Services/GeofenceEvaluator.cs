using Vespera.Domain.Attendance;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Services;

public sealed class GeofenceEvaluator
{
    private readonly IDistanceCalculator _distanceCalculator;

    public GeofenceEvaluator(IDistanceCalculator distanceCalculator)
    {
        ArgumentNullException.ThrowIfNull(distanceCalculator);
        _distanceCalculator = distanceCalculator;
    }

    /// <summary>A point exactly on the boundary (distance == radius) counts as inside.</summary>
    public bool IsInside(GeofenceZone zone, GeoCoordinate point)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(point);

        var distance = _distanceCalculator.DistanceMetres(zone.Center, point);
        return distance <= zone.RadiusMetres;
    }
}
