using Vespera.Application.Abstractions.Services;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Infrastructure.Notifications.Push;

/// <summary>The one <see cref="IPushSender"/> Application-facing implementation — routes to FCM
/// (Android/Web) or APNs (iOS) by <see cref="PushMessage.Platform"/>. A third platform is one more
/// <c>case</c> plus one more injected sender, never a change to <see cref="IPushSender"/> itself or
/// to <c>PushNotificationChannel</c> (OCP).</summary>
public sealed class CompositePushSender : IPushSender
{
    private readonly FcmPushSender _fcmPushSender;
    private readonly ApnsPushSender _apnsPushSender;

    public CompositePushSender(FcmPushSender fcmPushSender, ApnsPushSender apnsPushSender)
    {
        _fcmPushSender = fcmPushSender;
        _apnsPushSender = apnsPushSender;
    }

    public Task SendAsync(PushMessage message, CancellationToken cancellationToken) => message.Platform switch
    {
        DevicePlatform.Ios => _apnsPushSender.SendAsync(message, cancellationToken),
        DevicePlatform.Android or DevicePlatform.Web => _fcmPushSender.SendAsync(message, cancellationToken),
        _ => Task.CompletedTask,
    };
}
