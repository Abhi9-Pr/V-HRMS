using FluentAssertions;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Services;

public class HaversineDistanceCalculatorTests
{
    [Fact]
    public void DistanceMetres_Should_Be_Zero_For_The_Same_Point()
    {
        var calculator = new HaversineDistanceCalculator();
        var point = GeoCoordinate.Create(12.9716, 77.5946).Value;

        calculator.DistanceMetres(point, point).Should().Be(0d);
    }

    [Fact]
    public void DistanceMetres_Should_Match_A_KnownApproximate_Distance()
    {
        // Bengaluru MG Road to Bengaluru Airport, roughly 35km apart.
        var calculator = new HaversineDistanceCalculator();
        var mgRoad = GeoCoordinate.Create(12.9756, 77.6068).Value;
        var airport = GeoCoordinate.Create(13.1986, 77.7066).Value;

        var distance = calculator.DistanceMetres(mgRoad, airport);

        distance.Should().BeInRange(24_000, 30_000);
    }
}
