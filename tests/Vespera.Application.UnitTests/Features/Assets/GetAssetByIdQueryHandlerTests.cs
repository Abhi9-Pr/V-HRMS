using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetAssetByIdQueryHandlerTests
{
    private readonly IReadRepository<Asset> _assets = Substitute.For<IReadRepository<Asset>>();
    private readonly IReadRepository<AssetAssignment> _assignments = Substitute.For<IReadRepository<AssetAssignment>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetAssetByIdQueryHandler CreateHandler() => new(_assets, _assignments, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Include_BookValue_And_Assignment_History()
    {
        var purchaseDate = new DateOnly(2026, 1, 1);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var asset = Asset.Create(_tenantId, "AST-001", "Laptop", Money.Of(120000m, Currency.Inr), purchaseDate, DateTimeOffset.UtcNow, "system").Value;
        asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), DateTimeOffset.UtcNow, "system");

        var assignment = AssetAssignment.Assign(_tenantId, asset.Id, EmployeeId.New(), DateTimeOffset.UtcNow);

        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(asset);
        _assignments.ListAsync(Arg.Any<AssetAssignmentsByAssetSpecification>(), Arg.Any<CancellationToken>()).Returns([assignment]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAssetByIdQuery(asset.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BookValue.Should().Be(70000m);
        result.Value.Assignments.Should().ContainSingle(a => a.Id == assignment.Id.Value);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Asset_Not_Found()
    {
        _assets.FirstOrDefaultAsync(Arg.Any<AssetByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Asset?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAssetByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
