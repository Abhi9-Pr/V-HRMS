using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Locations;

public class GetLocationByIdQueryHandlerTests
{
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetLocationByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetLocationByIdQueryHandler CreateHandler() => new(_locations, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Dto_When_Location_Exists()
    {
        var location = Location.Create(
            _tenantId, "Head Office", "1 MG Road", "Bengaluru", "India",
            GeoCoordinate.Create(12.9716, 77.5946).Value, "Asia/Kolkata", DateTimeOffset.UtcNow, "seed").Value;
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetLocationByIdQuery(location.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Head Office");
        result.Value.Latitude.Should().Be(12.9716);
        result.Value.TimeZoneId.Should().Be("Asia/Kolkata");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Location_Missing()
    {
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetLocationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("location.not_found");
    }
}
