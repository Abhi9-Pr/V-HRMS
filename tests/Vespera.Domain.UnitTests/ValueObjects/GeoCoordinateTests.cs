using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class GeoCoordinateTests
{
    [Fact]
    public void Create_Should_Succeed_For_Valid_Coordinates()
    {
        var result = GeoCoordinate.Create(12.9716, 77.5946);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    public void Create_Should_Fail_For_Latitude_Out_Of_Range(double latitude, double longitude)
    {
        GeoCoordinate.Create(latitude, longitude).IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Create_Should_Fail_For_Longitude_Out_Of_Range(double latitude, double longitude)
    {
        GeoCoordinate.Create(latitude, longitude).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Instances_With_The_Same_Coordinates_Should_Be_Equal()
    {
        var first = GeoCoordinate.Create(12.9716, 77.5946).Value;
        var second = GeoCoordinate.Create(12.9716, 77.5946).Value;
        var different = GeoCoordinate.Create(19.0760, 72.8777).Value;

        first.Should().Be(second);
        first.Should().NotBe(different);
    }
}
