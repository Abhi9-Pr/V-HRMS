using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Locations;

public class GetLocationsQueryHandlerTests
{
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var location = Location.Create(
            tenantId, "Head Office", "1 MG Road", "Bengaluru", "India",
            GeoCoordinate.Create(12.9716, 77.5946).Value, "Asia/Kolkata", DateTimeOffset.UtcNow, "system").Value;

        _locations.ListAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Location> { location });
        _locations.CountAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = new GetLocationsQueryHandler(_locations, _tenantContext);
        var query = new GetLocationsQuery(new PagedRequest());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto =>
            dto.Name == "Head Office" && dto.Id == location.Id.Value);
    }
}
