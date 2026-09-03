using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetAssetsQueryHandlerTests
{
    private readonly IReadRepository<Asset> _assets = Substitute.For<IReadRepository<Asset>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetAssetsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetAssetsQueryHandler CreateHandler() => new(_assets, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_Assets()
    {
        var asset = Asset.Create(_tenantId, "AST-001", "Laptop", Money.Of(120000m, Currency.Inr), new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow, "system").Value;

        _assets.ListAsync(Arg.Any<AssetsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([asset]);
        _assets.CountAsync(Arg.Any<AssetsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAssetsQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(dto => dto.Id == asset.Id.Value && dto.AssetTag == "AST-001");
        result.Value.TotalCount.Should().Be(1);
    }
}
