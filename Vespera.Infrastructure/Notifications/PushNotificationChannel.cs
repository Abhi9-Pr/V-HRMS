using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Mobile;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Infrastructure.Notifications;

/// <summary>The real <see cref="INotificationChannel"/> for push, replacing the Phase-13-gated
/// stub. Fans a notification out to every one of the recipient's active <see
/// cref="DeviceRegistration"/>s via <see cref="IPushSender"/> (FCM/APNs, selected per device by
/// <c>CompositePushSender</c>). Same Metadata-driven pattern as <c>EmailNotificationChannel</c>:
/// the tenant id and deep-link fields travel in <see cref="NotificationMessage.Metadata"/> (set by
/// whoever raises the notification — see <c>ApprovalStepAssignedNotificationHandler</c>), and a
/// message carrying none of them is simply not deliverable over this channel and is skipped.</summary>
public sealed class PushNotificationChannel : INotificationChannel
{
    private static readonly string[] DeepLinkMetadataKeys = ["entityType", "entityId", "deepLink"];

    private readonly IReadRepositoryAdmin<DeviceRegistration> _deviceRegistrationsAdmin;
    private readonly IPushSender _pushSender;

    public PushNotificationChannel(IReadRepositoryAdmin<DeviceRegistration> deviceRegistrationsAdmin, IPushSender pushSender)
    {
        _deviceRegistrationsAdmin = deviceRegistrationsAdmin;
        _pushSender = pushSender;
    }

    public NotificationChannelType Type => NotificationChannelType.Push;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue("tenantId", out var tenantIdRaw) || !Guid.TryParse(tenantIdRaw, out var tenantIdValue) ||
            !Guid.TryParse(message.RecipientId, out var userIdValue))
        {
            return;
        }

        var devices = await _deviceRegistrationsAdmin.ListIgnoringFiltersAsync(
            new ActiveDeviceRegistrationsByUserSpecification(new TenantId(tenantIdValue), new UserId(userIdValue)), cancellationToken);
        if (devices.Count == 0)
        {
            return;
        }

        var data = message.Metadata
            .Where(kvp => DeepLinkMetadataKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (var device in devices)
        {
            await _pushSender.SendAsync(new PushMessage(device.PushToken, device.Platform, message.Title, message.Body, data), cancellationToken);
        }
    }
}
