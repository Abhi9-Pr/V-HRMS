using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Locations;

public class CreateLocationCommandHandlerTests
{
    private readonly IWriteRepository<Location> _locations = Substitute.For<IWriteRepository<Location>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateLocationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateLocationCommandHandler CreateHandler() =>
        new(_locations, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_Location_And_Return_Its_Id_On_Success()
    {
        var handler = CreateHandler();
        var command = new CreateLocationCommand("Head Office", "1 MG Road", "Bengaluru", "India", 12.9716, 77.5946, "Asia/Kolkata", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _locations.Received(1).AddAsync(
            Arg.Is<Location>(l => l.Name == "Head Office" && l.TimeZoneId == "Asia/Kolkata"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Name_Is_Blank()
    {
        var handler = CreateHandler();
        var command = new CreateLocationCommand(" ", "1 MG Road", "Bengaluru", "India", 12.9716, 77.5946, "Asia/Kolkata", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _locations.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Latitude_Is_Out_Of_Range()
    {
        var handler = CreateHandler();
        var command = new CreateLocationCommand("Head Office", "1 MG Road", "Bengaluru", "India", 200, 77.5946, "Asia/Kolkata", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _locations.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
