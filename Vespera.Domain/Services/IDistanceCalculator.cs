using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Services;

public interface IDistanceCalculator
{
    public double DistanceMetres(GeoCoordinate first, GeoCoordinate second);
}
