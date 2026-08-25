using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance;

public readonly record struct QuarantinedBiometricPunchId(Guid Value)
{
    public static QuarantinedBiometricPunchId New() => new(Guid.NewGuid());
}

public enum QuarantinedBiometricPunchStatus
{
    Pending,
    Resolved,
}

/// <summary>
/// A punch reported by a <see cref="BiometricDevice"/> whose <c>DeviceUserId</c> matched no
/// <see cref="Employee.BiometricDeviceUserId"/> at ingestion time. Held here rather than dropped
/// so HR can map it to the right employee retroactively via <see cref="Resolve"/>. Plain
/// <c>AggregateRoot&lt;TId&gt;</c> + <c>ITenantScoped</c> — no soft-delete/audit stamps needed,
/// same shape as <see cref="RegularizationRequest"/>.
/// </summary>
public sealed class QuarantinedBiometricPunch : AggregateRoot<QuarantinedBiometricPunchId>, ITenantScoped
{
    private QuarantinedBiometricPunch(
        QuarantinedBiometricPunchId id, TenantId tenantId, BiometricDeviceId biometricDeviceId,
        string deviceUserId, DateTimeOffset punchedAtUtc, PunchType? punchType, string externalRecordId)
        : base(id)
    {
        TenantId = tenantId;
        BiometricDeviceId = biometricDeviceId;
        DeviceUserId = deviceUserId;
        PunchedAtUtc = punchedAtUtc;
        PunchType = punchType;
        ExternalRecordId = externalRecordId;
        Status = QuarantinedBiometricPunchStatus.Pending;
    }

    public TenantId TenantId { get; }

    public BiometricDeviceId BiometricDeviceId { get; }

    public string DeviceUserId { get; }

    public DateTimeOffset PunchedAtUtc { get; }

    /// <summary>Null when the device didn't report a distinguishable in/out code — carried through
    /// from <see cref="Vespera.Application.Abstractions.Services.BiometricPunchRecord.PunchType"/>
    /// unchanged so <see cref="Resolve"/>'s caller can infer it the same way the poller would have,
    /// by toggling against the resolved employee's last known punch.</summary>
    public PunchType? PunchType { get; }

    /// <summary>The vendor's own record identifier, used to dedup a re-fetched punch across
    /// poller runs — the same value the poller checks before creating a real punch too.</summary>
    public string ExternalRecordId { get; }

    public QuarantinedBiometricPunchStatus Status { get; private set; }

    public EmployeeId? ResolvedEmployeeId { get; private set; }

    public static QuarantinedBiometricPunch Create(
        TenantId tenantId, BiometricDeviceId biometricDeviceId, string deviceUserId, DateTimeOffset punchedAtUtc,
        PunchType? punchType, string externalRecordId) =>
        new(QuarantinedBiometricPunchId.New(), tenantId, biometricDeviceId, deviceUserId, punchedAtUtc, punchType, externalRecordId);

    public Result Resolve(EmployeeId employeeId)
    {
        if (Status == QuarantinedBiometricPunchStatus.Resolved)
        {
            return Result.Failure(Error.Conflict("quarantined_biometric_punch.already_resolved", "This quarantined punch has already been resolved."));
        }

        Status = QuarantinedBiometricPunchStatus.Resolved;
        ResolvedEmployeeId = employeeId;
        return Result.Success();
    }
}
