using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordAssetReceivedCommandHandlerTests
{
    private readonly IReadRepository<AssetRecovery> _recoveries = Substitute.For<IReadRepository<AssetRecovery>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public RecordAssetReceivedCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private RecordAssetReceivedCommandHandler CreateHandler() => new(_recoveries, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Record_Received()
    {
        var recovery = AssetRecovery.Initiate(TenantId.New(), AssetAssignmentId.New(), AssetId.New(), EmployeeId.New(), DateTimeOffset.UtcNow);
        _recoveries.FirstOrDefaultAsync(Arg.Any<AssetRecoveryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(recovery);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordAssetReceivedCommand(recovery.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.Received);
    }
}
