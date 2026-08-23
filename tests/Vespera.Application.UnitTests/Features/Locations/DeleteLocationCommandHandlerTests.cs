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

public class DeleteLocationCommandHandlerTests
{
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public DeleteLocationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private DeleteLocationCommandHandler CreateHandler() => new(_locations, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Soft_Delete_When_Location_Exists()
    {
        var location = Location.Create(
            _tenantId, "Head Office", "1 MG Road", "Bengaluru", "India",
            GeoCoordinate.Create(12.9716, 77.5946).Value, "Asia/Kolkata", DateTimeOffset.UtcNow, "seed").Value;
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteLocationCommand(location.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        location.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Location_Missing()
    {
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteLocationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("location.not_found");
    }
}
