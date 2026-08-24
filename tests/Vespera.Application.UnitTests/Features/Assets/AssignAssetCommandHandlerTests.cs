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

public class AssignAssetCommandHandlerTests
{
    private readonly IReadRepository<Asset> _assets = Substitute.For<IReadRepository<Asset>>();
    private readonly IWriteRepository<AssetAssignment> _assignments = Substitute.For<IWriteRepository<AssetAssignment>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public AssignAssetCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private AssignAssetCommandHandler CreateHandler() => new(_assets, _assignments, _tenantContext, _currentUser, _dateTimeProvider);

    private Asset CreateAsset() => Asset.Create(
        _tenantId, "AST-001", "Laptop", Money.Of(80000m, Currency.Inr), new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow, "system").Value;

    [Fact]
    public async Task Handle_Should_Mark_Asset_Assigned_And_Create_Assignment()
    {
        var asset = CreateAsset();
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(asset);

        var handler = CreateHandler();
        var result = await handler.Handle(new AssignAssetCommand(asset.Id.Value, Guid.NewGuid(), null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(AssetStatus.Assigned);
        await _assignments.Received(1).AddAsync(Arg.Any<AssetAssignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Asset_Already_Assigned()
    {
        var asset = CreateAsset();
        asset.MarkAssigned(DateTimeOffset.UtcNow, "system");
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(asset);

        var handler = CreateHandler();
        var result = await handler.Handle(new AssignAssetCommand(asset.Id.Value, Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _assignments.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
