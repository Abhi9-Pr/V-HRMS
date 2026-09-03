using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetPendingAssetRecoveriesQueryHandlerTests
{
    private readonly IReadRepository<AssetRecovery> _recoveries = Substitute.For<IReadRepository<AssetRecovery>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetPendingAssetRecoveriesQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetPendingAssetRecoveriesQueryHandler CreateHandler() => new(_recoveries, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_Pending_Recoveries()
    {
        var recovery = AssetRecovery.Initiate(_tenantId, AssetAssignmentId.New(), AssetId.New(), EmployeeId.New(), DateTimeOffset.UtcNow);

        _recoveries.ListAsync(Arg.Any<PendingAssetRecoveriesSpecification>(), Arg.Any<CancellationToken>()).Returns([recovery]);
        _recoveries.CountAsync(Arg.Any<PendingAssetRecoveriesSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPendingAssetRecoveriesQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(dto => dto.Id == recovery.Id.Value && dto.Status == AssetRecoveryStatus.Pending);
        result.Value.TotalCount.Should().Be(1);
    }
}
