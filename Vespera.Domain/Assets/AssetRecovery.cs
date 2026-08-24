using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Assets;

public readonly record struct AssetRecoveryId(Guid Value)
{
    public static AssetRecoveryId New() => new(Guid.NewGuid());
}

public enum AssetRecoveryStatus
{
    Pending,
    InTransit,
    Received,
    DamageAssessed,
    WrittenOff,
    Completed,
}

/// <summary>Tracks the physical-recovery sub-process for one <see cref="AssetAssignment"/> during
/// offboarding — courier tracking, damage assessment, and write-off — separately from the generic
/// <see cref="OffboardingChecklist"/>, which just tracks that "recover this asset" is a task to
/// complete.</summary>
public sealed class AssetRecovery : AggregateRoot<AssetRecoveryId>, ITenantScoped
{
    private AssetRecovery(
        AssetRecoveryId id, TenantId tenantId, AssetAssignmentId assetAssignmentId, AssetId assetId, EmployeeId employeeId,
        DateTimeOffset initiatedAt)
        : base(id)
    {
        TenantId = tenantId;
        AssetAssignmentId = assetAssignmentId;
        AssetId = assetId;
        EmployeeId = employeeId;
        InitiatedAt = initiatedAt;
        Status = AssetRecoveryStatus.Pending;
    }

    public TenantId TenantId { get; }

    public AssetAssignmentId AssetAssignmentId { get; }

    public AssetId AssetId { get; }

    public EmployeeId EmployeeId { get; }

    public DateTimeOffset InitiatedAt { get; }

    public AssetRecoveryStatus Status { get; private set; }

    public string? CourierCarrier { get; private set; }

    public string? CourierTrackingReference { get; private set; }

    public DateTimeOffset? ReceivedAt { get; private set; }

    public string? DamageAssessmentNotes { get; private set; }

    public Money? WriteOffAmount { get; private set; }

    public string? WriteOffReason { get; private set; }

    public static AssetRecovery Initiate(
        TenantId tenantId, AssetAssignmentId assetAssignmentId, AssetId assetId, EmployeeId employeeId, DateTimeOffset occurredOn) =>
        new(AssetRecoveryId.New(), tenantId, assetAssignmentId, assetId, employeeId, occurredOn);

    public Result RecordCourierDispatch(string carrier, string trackingReference)
    {
        if (Status != AssetRecoveryStatus.Pending)
        {
            return Result.Failure(Error.Conflict("asset_recovery.not_pending", "Courier dispatch can only be recorded while recovery is Pending."));
        }

        if (string.IsNullOrWhiteSpace(carrier) || string.IsNullOrWhiteSpace(trackingReference))
        {
            return Result.Failure(Error.Validation("asset_recovery.courier_details_required", "Carrier and tracking reference are required."));
        }

        CourierCarrier = carrier.Trim();
        CourierTrackingReference = trackingReference.Trim();
        Status = AssetRecoveryStatus.InTransit;
        return Result.Success();
    }

    public Result RecordReceived(DateTimeOffset occurredOn)
    {
        if (Status != AssetRecoveryStatus.Pending && Status != AssetRecoveryStatus.InTransit)
        {
            return Result.Failure(Error.Conflict("asset_recovery.not_recoverable", "This asset has already been marked received or closed."));
        }

        ReceivedAt = occurredOn;
        Status = AssetRecoveryStatus.Received;
        return Result.Success();
    }

    public Result RecordDamageAssessment(string notes)
    {
        if (Status != AssetRecoveryStatus.Received)
        {
            return Result.Failure(Error.Conflict("asset_recovery.not_received", "Damage can only be assessed after the asset is received."));
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            return Result.Failure(Error.Validation("asset_recovery.damage_notes_required", "Damage assessment notes are required."));
        }

        DamageAssessmentNotes = notes.Trim();
        Status = AssetRecoveryStatus.DamageAssessed;
        return Result.Success();
    }

    public Result WriteOff(Money amount, string reason)
    {
        if (Status != AssetRecoveryStatus.Received && Status != AssetRecoveryStatus.DamageAssessed)
        {
            return Result.Failure(Error.Conflict("asset_recovery.not_writeoffable", "An asset can only be written off after it is received."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("asset_recovery.writeoff_reason_required", "A write-off reason is required."));
        }

        if (amount.Amount < 0)
        {
            return Result.Failure(Error.Validation("asset_recovery.writeoff_amount_invalid", "Write-off amount cannot be negative."));
        }

        WriteOffAmount = amount;
        WriteOffReason = reason.Trim();
        Status = AssetRecoveryStatus.WrittenOff;
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status is AssetRecoveryStatus.Pending or AssetRecoveryStatus.InTransit)
        {
            return Result.Failure(Error.Conflict("asset_recovery.not_ready", "The asset must be received before recovery can be completed."));
        }

        if (Status == AssetRecoveryStatus.Completed)
        {
            return Result.Failure(Error.Conflict("asset_recovery.already_completed", "This recovery is already completed."));
        }

        Status = AssetRecoveryStatus.Completed;
        return Result.Success();
    }
}
