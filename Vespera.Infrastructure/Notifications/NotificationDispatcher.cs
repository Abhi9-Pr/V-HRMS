using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Notifications;

/// <summary>
/// Fans a notification out across every registered <see cref="INotificationChannel"/>. No channel
/// (email/SMS/push/in-app) is implemented yet — that's a separate Infrastructure responsibility
/// per AGENTS.md — so today this is a correct no-op: it dispatches to zero channels rather than
/// failing, and a channel becomes active purely by registering an <see cref="INotificationChannel"/>
/// (OCP; this class never changes).
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;

    public NotificationDispatcher(IEnumerable<INotificationChannel> channels)
    {
        _channels = channels;
    }

    public async Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        foreach (var channel in _channels)
        {
            await channel.SendAsync(message, cancellationToken);
        }
    }
}
