using Vespera.Domain.Common;

namespace Vespera.Domain.IdentityAccess;

public readonly record struct DeviceRegistrationId(Guid Value)
{
    public static DeviceRegistrationId New() => new(Guid.NewGuid());
}

public enum DevicePlatform
{
    Ios,
    Android,
    Web,
}

public sealed class DeviceRegistration : AggregateRoot<DeviceRegistrationId>, ITenantScoped
{
    private DeviceRegistration(
        DeviceRegistrationId id, TenantId tenantId, UserId userId, string deviceId, DevicePlatform platform, string pushToken)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        DeviceId = deviceId;
        Platform = platform;
        PushToken = pushToken;
        IsActive = true;
    }

    public TenantId TenantId { get; }

    public UserId UserId { get; }

    public string DeviceId { get; }

    public DevicePlatform Platform { get; }

    public string PushToken { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<DeviceRegistration> Register(
        TenantId tenantId, UserId userId, string deviceId, DevicePlatform platform, string pushToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return Result.Failure<DeviceRegistration>(
                Error.Validation("device_registration.device_id_required", "Device id is required."));
        }

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return Result.Failure<DeviceRegistration>(
                Error.Validation("device_registration.push_token_required", "Push token is required."));
        }

        return Result.Success(new DeviceRegistration(
            DeviceRegistrationId.New(), tenantId, userId, deviceId.Trim(), platform, pushToken));
    }

    public Result UpdatePushToken(string pushToken)
    {
        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return Result.Failure(Error.Validation("device_registration.push_token_required", "Push token is required."));
        }

        PushToken = pushToken;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("device_registration.already_inactive", "Device registration is already inactive."));
        }

        IsActive = false;
        return Result.Success();
    }
}
