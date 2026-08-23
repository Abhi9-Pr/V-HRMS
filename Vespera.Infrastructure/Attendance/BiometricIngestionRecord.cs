namespace Vespera.Infrastructure.Attendance;

/// <summary>
/// One row per biometric punch record the poller has ever processed, regardless of whether it
/// became a real <c>AttendancePunch</c> or a <c>QuarantinedBiometricPunch</c> — a pure
/// persistence model, not a Domain aggregate, same spirit as
/// <c>Vespera.Infrastructure.Persistence.Auditing.PiiAccessAuditEntry</c>. Its only job is the
/// dedup check <see cref="BiometricPunchPollerHostedService"/> runs before processing a
/// re-fetched record — a real punch's own domain type carries no vendor record id, and a
/// quarantine entry's uniqueness constraint alone can't cover the case where the *same* external
/// record is re-fetched after having already been resolved into a real punch.
/// </summary>
public sealed class BiometricIngestionRecord
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BiometricDeviceId { get; set; }

    public string ExternalRecordId { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}
