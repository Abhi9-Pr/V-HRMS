using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordDamageAssessmentCommandHandlerTests
{
    private readonly IReadRepository<AssetRecovery> _recoveries = Substitute.For<IReadRepository<AssetRecovery>>();

    private RecordDamageAssessmentCommandHandler CreateHandler() => new(_recoveries);

    [Fact]
    public async Task Handle_Should_Fail_Before_Received()
    {
        var recovery = AssetRecovery.Initiate(TenantId.New(), AssetAssignmentId.New(), AssetId.New(), EmployeeId.New(), DateTimeOffset.UtcNow);
        _recoveries.FirstOrDefaultAsync(Arg.Any<AssetRecoveryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(recovery);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordDamageAssessmentCommand(recovery.Id.Value, "Cracked", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Succeed_After_Received()
    {
        var recovery = AssetRecovery.Initiate(TenantId.New(), AssetAssignmentId.New(), AssetId.New(), EmployeeId.New(), DateTimeOffset.UtcNow);
        recovery.RecordReceived(DateTimeOffset.UtcNow);
        _recoveries.FirstOrDefaultAsync(Arg.Any<AssetRecoveryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(recovery);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordDamageAssessmentCommand(recovery.Id.Value, "Cracked", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.DamageAssessed);
    }
}
