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

public class ConfigureAssetDepreciationCommandHandlerTests
{
    private readonly IReadRepository<Asset> _assets = Substitute.For<IReadRepository<Asset>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public ConfigureAssetDepreciationCommandHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private ConfigureAssetDepreciationCommandHandler CreateHandler() => new(_assets, _currentUser, _dateTimeProvider);

    private static Asset CreateAsset() => Asset.Create(
        TenantId.New(), "AST-001", "Laptop", Money.Of(120000m, Currency.Inr), new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow, "system").Value;

    [Fact]
    public async Task Handle_Should_Configure_Depreciation_On_Success()
    {
        var asset = CreateAsset();
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(asset);

        var handler = CreateHandler();
        var command = new ConfigureAssetDepreciationCommand(asset.Id.Value, DepreciationMethod.StraightLine, 24, 20000m, Currency.Inr, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset.Depreciation.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Asset_Not_Found()
    {
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Asset?)null);

        var handler = CreateHandler();
        var command = new ConfigureAssetDepreciationCommand(Guid.NewGuid(), DepreciationMethod.StraightLine, 24, 20000m, Currency.Inr, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
