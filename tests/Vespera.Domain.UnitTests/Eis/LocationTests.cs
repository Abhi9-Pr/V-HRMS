using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class LocationTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly GeoCoordinate Coordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;

    [Fact]
    public void Create_Should_Succeed_And_Trim_Name()
    {
        var result = Location.Create(TenantId, "  Head Office  ", "1 Main St", "Bengaluru", "India", Coordinate, "Asia/Kolkata", Now, "system");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Head Office");
        result.Value.AddressLine.Should().Be("1 Main St");
        result.Value.City.Should().Be("Bengaluru");
        result.Value.Country.Should().Be("India");
        result.Value.Coordinate.Should().Be(Coordinate);
        result.Value.TimeZoneId.Should().Be("Asia/Kolkata");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Name_Is_Missing(string name)
    {
        var result = Location.Create(TenantId, name, "1 Main St", "Bengaluru", "India", Coordinate, "Asia/Kolkata", Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("location.name_required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_TimeZoneId_Is_Missing(string timeZoneId)
    {
        var result = Location.Create(TenantId, "Head Office", "1 Main St", "Bengaluru", "India", Coordinate, timeZoneId, Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("location.timezone_required");
    }

    [Fact]
    public void Relocate_Should_Update_Coordinate_And_Address_Fields()
    {
        var location = Location.Create(TenantId, "Head Office", "1 Main St", "Bengaluru", "India", Coordinate, "Asia/Kolkata", Now, "system").Value;
        var newCoordinate = GeoCoordinate.Create(19.0760, 72.8777).Value;

        var result = location.Relocate(newCoordinate, "2 Marine Drive", "Mumbai", "India", Now.AddDays(1), "admin");

        result.IsSuccess.Should().BeTrue();
        location.Coordinate.Should().Be(newCoordinate);
        location.AddressLine.Should().Be("2 Marine Drive");
        location.City.Should().Be("Mumbai");
        location.Country.Should().Be("India");
    }
}
