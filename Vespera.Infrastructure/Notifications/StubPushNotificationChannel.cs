using Microsoft.Extensions.Logging;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Notifications;

/// <summary>Push notifications are out of scope until Phase 13 (the Capacitor mobile shell, which is
/// what would actually hold a device token to push to). Registered now, as a real
/// <see cref="INotificationChannel"/>, purely to prove the dispatcher needs no changes when Phase 13
/// swaps this for a real APNs/FCM sender — until then it just logs and no-ops.</summary>
public sealed class StubPushNotificationChannel : INotificationChannel
{
    private static readonly Action<ILogger, string, Exception?> LogNotSent = LoggerMessage.Define<string>(
        LogLevel.Debug, new EventId(1, nameof(LogNotSent)), "Push notification to {RecipientId} not sent — push is stubbed until Phase 13.");

    private readonly ILogger<StubPushNotificationChannel> _logger;

    public StubPushNotificationChannel(ILogger<StubPushNotificationChannel> logger)
    {
        _logger = logger;
    }

    public NotificationChannelType Type => NotificationChannelType.Push;

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        LogNotSent(_logger, message.RecipientId, null);
        return Task.CompletedTask;
    }
}
