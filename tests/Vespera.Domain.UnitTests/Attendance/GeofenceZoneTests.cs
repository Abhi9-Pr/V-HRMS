using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Attendance;

public class GeofenceZoneTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly LocationId LocationId = LocationId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly GeoCoordinate Center = GeoCoordinate.Create(28.6139, 77.2090).Value;

    [Fact]
    public void Create_Should_Succeed_With_Valid_Fields()
    {
        var result = GeofenceZone.Create(TenantId, LocationId, "HQ Campus", Center, 200, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("HQ Campus");
        result.Value.LocationId.Should().Be(LocationId);
        result.Value.Center.Should().Be(Center);
        result.Value.RadiusMetres.Should().Be(200);
    }

    [Fact]
    public void Create_Should_Fail_When_Name_Is_Blank()
    {
        var result = GeofenceZone.Create(TenantId, LocationId, "  ", Center, 200, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("geofence_zone.name_required");
    }

    [Fact]
    public void Create_Should_Fail_For_A_NonPositive_Radius()
    {
        var result = GeofenceZone.Create(TenantId, LocationId, "HQ Campus", Center, 0, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("geofence_zone.invalid_radius");
    }

    [Fact]
    public void Resize_Should_Update_RadiusMetres()
    {
        var zone = CreateZone();

        var result = zone.Resize(500, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        zone.RadiusMetres.Should().Be(500);
    }

    [Fact]
    public void Resize_Should_Fail_For_A_NonPositive_Radius()
    {
        var zone = CreateZone();

        var result = zone.Resize(-1, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("geofence_zone.invalid_radius");
        zone.RadiusMetres.Should().Be(200);
    }

    [Fact]
    public void Relocate_Should_Update_Center()
    {
        var zone = CreateZone();
        var newCenter = GeoCoordinate.Create(19.0760, 72.8777).Value;

        var result = zone.Relocate(newCenter, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        zone.Center.Should().Be(newCenter);
    }

    [Fact]
    public void New_Ids_Should_Be_Distinct()
    {
        GeofenceZoneId.New().Should().NotBe(GeofenceZoneId.New());
    }

    private static GeofenceZone CreateZone() =>
        GeofenceZone.Create(TenantId, LocationId, "HQ Campus", Center, 200, Now, "hr@vespera.test").Value;
}
