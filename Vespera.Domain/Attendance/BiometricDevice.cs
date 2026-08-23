using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance;

public readonly record struct BiometricDeviceId(Guid Value)
{
    public static BiometricDeviceId New() => new(Guid.NewGuid());
}

/// <summary>The vendor protocol a registered device speaks. Adding a second vendor is one new
/// member here plus one new <c>IBiometricDeviceAdapter</c> implementation — see
/// docs/adding-a-biometric-vendor.md.</summary>
public enum BiometricVendorType
{
    ZKTeco,
}

/// <summary>
/// A registered biometric attendance device/gateway. Deliberately duplicates the connection
/// shape of <c>Vespera.Application.Abstractions.Services.BiometricDeviceConnection</c> rather than
/// referencing that Application-layer type directly — Domain cannot depend on Application (see
/// DependencyRuleTests). The Infrastructure poller reads <see cref="Host"/>/<see cref="Port"/>/
/// <see cref="ApiKeyConfigurationKey"/> off this aggregate to build that record itself. Never
/// holds a raw credential — <see cref="ApiKeyConfigurationKey"/> only names where the real secret
/// lives (configuration/user-secrets), the same discipline <c>BiometricDeviceConnection</c>'s own
/// doc comment establishes.
/// </summary>
public sealed class BiometricDevice : AuditableTenantAggregateRoot<BiometricDeviceId>
{
    private BiometricDevice(
        BiometricDeviceId id, TenantId tenantId, LocationId locationId, BiometricVendorType vendorType,
        string host, int port, string? apiKeyConfigurationKey, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        LocationId = locationId;
        VendorType = vendorType;
        Host = host;
        Port = port;
        ApiKeyConfigurationKey = apiKeyConfigurationKey;
        IsActive = true;
    }

    public LocationId LocationId { get; }

    public BiometricVendorType VendorType { get; }

    public string Host { get; private set; }

    public int Port { get; private set; }

    public string? ApiKeyConfigurationKey { get; private set; }

    /// <summary>The adapter's opaque per-device sync position. Null means "sync from the
    /// beginning" — the poller never inspects or derives this itself, only persists whatever the
    /// adapter last returned.</summary>
    public string? Cursor { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<BiometricDevice> Register(
        TenantId tenantId, LocationId locationId, BiometricVendorType vendorType, string host, int port,
        string? apiKeyConfigurationKey, DateTimeOffset createdAt, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return Result.Failure<BiometricDevice>(Error.Validation("biometric_device.host_required", "Device host is required."));
        }

        if (port is <= 0 or > 65535)
        {
            return Result.Failure<BiometricDevice>(Error.Validation("biometric_device.invalid_port", "Port must be between 1 and 65535."));
        }

        return Result.Success(new BiometricDevice(
            BiometricDeviceId.New(), tenantId, locationId, vendorType, host.Trim(), port, apiKeyConfigurationKey, createdAt, createdBy));
    }

    public Result UpdateCursor(string? cursor, DateTimeOffset occurredOn, string modifiedBy)
    {
        Cursor = cursor;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Deactivate(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("biometric_device.already_inactive", "This device is already inactive."));
        }

        IsActive = false;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
