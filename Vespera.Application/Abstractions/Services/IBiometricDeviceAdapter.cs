using Vespera.Domain.Attendance;

namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// A punch reported by a biometric device, normalized to a vendor-agnostic shape. Devices don't
/// always report a distinguishable in/out code — <paramref name="PunchType"/> is null when the
/// adapter can't determine one and the caller must infer it (e.g. by toggling against the
/// employee's last known punch).
/// </summary>
public sealed record BiometricPunchRecord(
    string DeviceUserId, DateTimeOffset PunchedAtUtc, PunchType? PunchType, string ExternalRecordId);

/// <summary>
/// Connection details for one registered device. Deliberately holds a reference to where the
/// credential lives (a configuration/secret key), never the credential itself — this record is
/// passed around in memory and logged in error paths, so it must stay safe to log.
/// </summary>
public sealed record BiometricDeviceConnection(string Host, int Port, string? ApiKeyConfigurationKey);

/// <summary>One page of newly-fetched punches plus the adapter's own opaque cursor for the next
/// call — the poller persists <see cref="NextCursor"/> verbatim, it never inspects or derives
/// one itself, since cursor semantics are vendor-specific.</summary>
public sealed record BiometricFetchResult(IReadOnlyList<BiometricPunchRecord> Records, string? NextCursor);

/// <summary>
/// The port every biometric-device vendor integration implements. A new vendor is one new class
/// implementing this interface plus one DI registration line — see
/// docs/adding-a-biometric-vendor.md for the concrete checklist.
/// </summary>
public interface IBiometricDeviceAdapter
{
    public Task<BiometricFetchResult> FetchSinceAsync(
        BiometricDeviceConnection connection, string? cursor, CancellationToken cancellationToken);
}
