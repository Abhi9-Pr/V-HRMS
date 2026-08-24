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

public class ReturnAssetCommandHandlerTests
{
    private readonly IReadRepository<AssetAssignment> _assignments = Substitute.For<IReadRepository<AssetAssignment>>();
    private readonly IReadRepository<Asset> _assets = Substitute.For<IReadRepository<Asset>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public ReturnAssetCommandHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private ReturnAssetCommandHandler CreateHandler() => new(_assignments, _assets, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Return_Assignment_And_Restock_Asset()
    {
        var asset = Asset.Create(
            _tenantId, "AST-001", "Laptop", Money.Of(80000m, Currency.Inr), new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow, "system").Value;
        asset.MarkAssigned(DateTimeOffset.UtcNow, "system");
        var assignment = AssetAssignment.Assign(_tenantId, asset.Id, EmployeeId.New(), DateTimeOffset.UtcNow);

        _assignments.FirstOrDefaultAsync(Arg.Any<AssetAssignmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(assignment);
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(asset);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReturnAssetCommand(assignment.Id.Value, "Good condition", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.ReturnedAt.Should().NotBeNull();
        asset.Status.Should().Be(AssetStatus.InStock);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Assignment_Not_Found()
    {
        _assignments.FirstOrDefaultAsync(Arg.Any<AssetAssignmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((AssetAssignment?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReturnAssetCommand(Guid.NewGuid(), "Good", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
