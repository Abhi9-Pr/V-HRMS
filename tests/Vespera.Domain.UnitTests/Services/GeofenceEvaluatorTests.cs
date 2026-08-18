using FluentAssertions;
using NSubstitute;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Services;

public class GeofenceEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsInside_Should_Be_True_When_The_Point_Is_Exactly_On_The_Boundary()
    {
        var zone = CreateZone(radiusMetres: 100);
        var distanceCalculator = Substitute.For<IDistanceCalculator>();
        distanceCalculator.DistanceMetres(Arg.Any<GeoCoordinate>(), Arg.Any<GeoCoordinate>()).Returns(100d);
        var evaluator = new GeofenceEvaluator(distanceCalculator);

        var result = evaluator.IsInside(zone, zone.Center);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsInside_Should_Be_True_When_Comfortably_Within_The_Radius()
    {
        var zone = CreateZone(radiusMetres: 100);
        var distanceCalculator = Substitute.For<IDistanceCalculator>();
        distanceCalculator.DistanceMetres(Arg.Any<GeoCoordinate>(), Arg.Any<GeoCoordinate>()).Returns(50d);
        var evaluator = new GeofenceEvaluator(distanceCalculator);

        evaluator.IsInside(zone, zone.Center).Should().BeTrue();
    }

    [Fact]
    public void IsInside_Should_Be_False_When_Just_Outside_The_Radius()
    {
        var zone = CreateZone(radiusMetres: 100);
        var distanceCalculator = Substitute.For<IDistanceCalculator>();
        distanceCalculator.DistanceMetres(Arg.Any<GeoCoordinate>(), Arg.Any<GeoCoordinate>()).Returns(100.01d);
        var evaluator = new GeofenceEvaluator(distanceCalculator);

        evaluator.IsInside(zone, zone.Center).Should().BeFalse();
    }

    private static GeofenceZone CreateZone(double radiusMetres)
    {
        var center = GeoCoordinate.Create(12.9716, 77.5946).Value;
        return GeofenceZone.Create(TenantId.New(), "HQ", center, radiusMetres, Now, "admin@vespera.test").Value;
    }
}
