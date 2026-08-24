using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Assets;

public class WriteOffAssetCommandHandlerTests
{
    private readonly IReadRepository<AssetRecovery> _recoveries = Substitute.For<IReadRepository<AssetRecovery>>();
    private readonly IReadRepository<Asset> _assets = Substitute.For<IReadRepository<Asset>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public WriteOffAssetCommandHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private WriteOffAssetCommandHandler CreateHandler() => new(_recoveries, _assets, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_WriteOff_Recovery_And_Retire_Asset()
    {
        var asset = Asset.Create(
            _tenantId, "AST-001", "Laptop", Money.Of(80000m, Currency.Inr), new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow, "system").Value;
        var recovery = AssetRecovery.Initiate(_tenantId, AssetAssignmentId.New(), asset.Id, EmployeeId.New(), DateTimeOffset.UtcNow);
        recovery.RecordReceived(DateTimeOffset.UtcNow);

        _recoveries.FirstOrDefaultAsync(Arg.Any<AssetRecoveryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(recovery);
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(asset);

        var handler = CreateHandler();
        var command = new WriteOffAssetCommand(recovery.Id.Value, 0m, Currency.Inr, "Lost in transit", null);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.WrittenOff);
        asset.Status.Should().Be(AssetStatus.Retired);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Recovery_Not_Found()
    {
        _recoveries.FirstOrDefaultAsync(Arg.Any<AssetRecoveryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((AssetRecovery?)null);

        var handler = CreateHandler();
        var command = new WriteOffAssetCommand(Guid.NewGuid(), 0m, Currency.Inr, "Lost", null);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
