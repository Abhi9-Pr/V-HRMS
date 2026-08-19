namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// Fans a notification out across whichever <see cref="INotificationChannel"/> implementations
/// are registered (OCP: add a channel by registering a new INotificationChannel, never by
/// editing this contract).
/// </summary>
public interface INotificationDispatcher
{
    public Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken);
}
