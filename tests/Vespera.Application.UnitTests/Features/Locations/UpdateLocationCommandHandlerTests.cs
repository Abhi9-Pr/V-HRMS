using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Locations;

public class UpdateLocationCommandHandlerTests
{
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateLocationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateLocationCommandHandler CreateHandler() => new(_locations, _tenantContext, _currentUser, _dateTimeProvider);

    private Location CreateLocation() => Location.Create(
        _tenantId, "Head Office", "1 MG Road", "Bengaluru", "India",
        GeoCoordinate.Create(12.9716, 77.5946).Value, "Asia/Kolkata", DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Relocate_When_Location_Exists()
    {
        var location = CreateLocation();
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);

        var handler = CreateHandler();
        var command = new UpdateLocationCommand(location.Id.Value, 19.0760, 72.8777, "Bandra Kurla Complex", "Mumbai", "India");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        location.City.Should().Be("Mumbai");
        location.Coordinate.Latitude.Should().Be(19.0760);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Location_Missing()
    {
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateLocationCommand(Guid.NewGuid(), 19.0760, 72.8777, "Bandra Kurla Complex", "Mumbai", "India"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("location.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Latitude_Is_Out_Of_Range()
    {
        var location = CreateLocation();
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);

        var handler = CreateHandler();
        var command = new UpdateLocationCommand(location.Id.Value, 200, 72.8777, "Bandra Kurla Complex", "Mumbai", "India");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("geo_coordinate.invalid_latitude");
    }
}
