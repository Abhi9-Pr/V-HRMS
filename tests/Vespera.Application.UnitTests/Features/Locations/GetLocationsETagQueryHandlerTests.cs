using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Locations;

public class GetLocationsETagQueryHandlerTests
{
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetLocationsETagQueryHandler CreateHandler() => new(_locations, _tenantContext);

    private Location CreateLocation(string name) =>
        Location.Create(_tenantId, name, "1 Main St", "Bengaluru", "India", GeoCoordinate.Create(12.9, 77.5).Value, "Asia/Kolkata", DateTimeOffset.UtcNow, "system").Value;

    [Fact]
    public async Task Handle_Should_Return_The_Same_Hash_For_The_Same_Locations()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var location = CreateLocation("Head Office");
        _locations.ListAsync(Arg.Any<LocationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([location]);

        var first = await CreateHandler().Handle(new GetLocationsETagQuery(), CancellationToken.None);
        var second = await CreateHandler().Handle(new GetLocationsETagQuery(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.Should().NotBeNullOrEmpty();
        second.Value.Should().Be(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Different_Hash_When_The_Location_Set_Differs()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var locationA = CreateLocation("Head Office");
        var locationB = CreateLocation("Branch Office");

        _locations.ListAsync(Arg.Any<LocationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([locationA]);
        var first = await CreateHandler().Handle(new GetLocationsETagQuery(), CancellationToken.None);

        _locations.ListAsync(Arg.Any<LocationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([locationA, locationB]);
        var second = await CreateHandler().Handle(new GetLocationsETagQuery(), CancellationToken.None);

        second.Value.Should().NotBe(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Hash_Even_When_There_Are_No_Locations()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _locations.ListAsync(Arg.Any<LocationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetLocationsETagQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }
}
