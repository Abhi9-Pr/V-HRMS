using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Assets;

public class CreateAssetCommandHandlerTests
{
    private readonly IWriteRepository<Asset> _assets = Substitute.For<IWriteRepository<Asset>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateAssetCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateAssetCommandHandler CreateHandler() => new(_assets, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_Asset_And_Return_Its_Id_On_Success()
    {
        var handler = CreateHandler();
        var command = new CreateAssetCommand(
            "AST-001", "Laptop", 80000m, Currency.Inr, new DateOnly(2026, 1, 1), "SN-123", "AA:BB:CC:DD:EE:FF", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _assets.Received(1).AddAsync(Arg.Is<Asset>(a => a.AssetTag == "AST-001" && a.SerialNumber == "SN-123"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Tag_Is_Blank()
    {
        var handler = CreateHandler();
        var command = new CreateAssetCommand(" ", "Laptop", 80000m, Currency.Inr, new DateOnly(2026, 1, 1), null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _assets.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
