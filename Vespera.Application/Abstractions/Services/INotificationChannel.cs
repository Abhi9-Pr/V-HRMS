namespace Vespera.Application.Abstractions.Services;

public enum NotificationChannelType
{
    Email,
    Sms,
    Push,
    InApp,
}

public sealed record NotificationMessage(string RecipientId, string Title, string Body, IReadOnlyDictionary<string, string> Metadata);

public interface INotificationChannel
{
    public NotificationChannelType Type { get; }

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
