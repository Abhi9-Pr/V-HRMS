using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordCourierDispatchCommandHandlerTests
{
    private readonly IReadRepository<AssetRecovery> _recoveries = Substitute.For<IReadRepository<AssetRecovery>>();

    private RecordCourierDispatchCommandHandler CreateHandler() => new(_recoveries);

    private static AssetRecovery CreateRecovery() =>
        AssetRecovery.Initiate(TenantId.New(), AssetAssignmentId.New(), AssetId.New(), EmployeeId.New(), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_Should_Record_Dispatch()
    {
        var recovery = CreateRecovery();
        _recoveries.FirstOrDefaultAsync(Arg.Any<AssetRecoveryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(recovery);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordCourierDispatchCommand(recovery.Id.Value, "BlueDart", "TRK-1", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.InTransit);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Recovery_Not_Found()
    {
        _recoveries.FirstOrDefaultAsync(Arg.Any<AssetRecoveryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((AssetRecovery?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordCourierDispatchCommand(Guid.NewGuid(), "BlueDart", "TRK-1", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
