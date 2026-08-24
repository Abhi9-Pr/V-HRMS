using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Assets;

public class AssetRecoveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static AssetRecovery CreateRecovery() =>
        AssetRecovery.Initiate(TenantId.New(), AssetAssignmentId.New(), AssetId.New(), EmployeeId.New(), Now);

    [Fact]
    public void Initiate_Should_Start_Pending()
    {
        var recovery = CreateRecovery();

        recovery.Status.Should().Be(AssetRecoveryStatus.Pending);
    }

    [Fact]
    public void RecordCourierDispatch_Should_Move_To_InTransit()
    {
        var recovery = CreateRecovery();

        var result = recovery.RecordCourierDispatch("BlueDart", "TRK-123");

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.InTransit);
        recovery.CourierCarrier.Should().Be("BlueDart");
    }

    [Fact]
    public void RecordCourierDispatch_Should_Fail_When_Not_Pending()
    {
        var recovery = CreateRecovery();
        recovery.RecordCourierDispatch("BlueDart", "TRK-123");

        var result = recovery.RecordCourierDispatch("DTDC", "TRK-999");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordReceived_Should_Succeed_Directly_From_Pending_For_Hand_Delivered_Returns()
    {
        var recovery = CreateRecovery();

        var result = recovery.RecordReceived(Now.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.Received);
    }

    [Fact]
    public void RecordReceived_Should_Succeed_From_InTransit()
    {
        var recovery = CreateRecovery();
        recovery.RecordCourierDispatch("BlueDart", "TRK-123");

        var result = recovery.RecordReceived(Now.AddDays(3));

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.Received);
    }

    [Fact]
    public void RecordDamageAssessment_Should_Fail_Before_Received()
    {
        var recovery = CreateRecovery();

        var result = recovery.RecordDamageAssessment("Cracked screen");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordDamageAssessment_Should_Succeed_After_Received()
    {
        var recovery = CreateRecovery();
        recovery.RecordReceived(Now.AddDays(1));

        var result = recovery.RecordDamageAssessment("Cracked screen");

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.DamageAssessed);
    }

    [Fact]
    public void WriteOff_Should_Succeed_From_Received_Without_Prior_Damage_Assessment()
    {
        var recovery = CreateRecovery();
        recovery.RecordReceived(Now.AddDays(1));

        var result = recovery.WriteOff(Money.Of(5000m, Currency.Inr), "Lost in transit");

        result.IsSuccess.Should().BeTrue();
        recovery.Status.Should().Be(AssetRecoveryStatus.WrittenOff);
    }

    [Fact]
    public void WriteOff_Should_Succeed_From_DamageAssessed()
    {
        var recovery = CreateRecovery();
        recovery.RecordReceived(Now.AddDays(1));
        recovery.RecordDamageAssessment("Cracked screen");

        var result = recovery.WriteOff(Money.Of(5000m, Currency.Inr), "Beyond repair");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void WriteOff_Should_Fail_Before_Received()
    {
        var recovery = CreateRecovery();

        var result = recovery.WriteOff(Money.Of(5000m, Currency.Inr), "Lost");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Complete_Should_Fail_While_Pending_Or_InTransit()
    {
        var recovery = CreateRecovery();

        recovery.Complete().IsFailure.Should().BeTrue();

        recovery.RecordCourierDispatch("BlueDart", "TRK-1");
        recovery.Complete().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Complete_Should_Succeed_From_Received_DamageAssessed_Or_WrittenOff()
    {
        var receivedRecovery = CreateRecovery();
        receivedRecovery.RecordReceived(Now.AddDays(1));
        receivedRecovery.Complete().IsSuccess.Should().BeTrue();

        var writtenOffRecovery = CreateRecovery();
        writtenOffRecovery.RecordReceived(Now.AddDays(1));
        writtenOffRecovery.WriteOff(Money.Of(1000m, Currency.Inr), "Damaged");
        writtenOffRecovery.Complete().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Complete_Should_Fail_When_Already_Completed()
    {
        var recovery = CreateRecovery();
        recovery.RecordReceived(Now.AddDays(1));
        recovery.Complete();

        var result = recovery.Complete();

        result.IsFailure.Should().BeTrue();
    }
}
