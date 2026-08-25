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

public class MarkAssetUnderRepairCommandHandlerTests
{
    private readonly IReadRepository<Asset> _assets = Substitute.For<IReadRepository<Asset>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public MarkAssetUnderRepairCommandHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private MarkAssetUnderRepairCommandHandler CreateHandler() => new(_assets, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Mark_Asset_Under_Repair()
    {
        var asset = Asset.Create(
            TenantId.New(), "AST-001", "Laptop", Money.Of(80000m, Currency.Inr), new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow, "system").Value;
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(asset);

        var handler = CreateHandler();
        var result = await handler.Handle(new MarkAssetUnderRepairCommand(asset.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(AssetStatus.UnderRepair);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Asset_Not_Found()
    {
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Asset?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new MarkAssetUnderRepairCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
