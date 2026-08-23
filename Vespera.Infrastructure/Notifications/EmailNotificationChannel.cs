using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Notifications;

/// <summary>One more <see cref="INotificationChannel"/> registration — the dispatcher (Application/
/// Infrastructure's <c>NotificationDispatcher</c>) already fans out to every registered channel
/// without modification, same as <c>SignalRNotificationChannel</c> proved for in-app. The
/// destination address travels in <see cref="NotificationMessage.Metadata"/> under "email" (set by
/// whoever builds the message — <see cref="NotificationMessage.RecipientId"/> is a user id, not
/// necessarily an address); a message with no email in its metadata simply isn't deliverable over
/// this channel and is skipped rather than failing the whole dispatch.</summary>
public sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly IEmailSender _emailSender;

    public EmailNotificationChannel(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public NotificationChannelType Type => NotificationChannelType.Email;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue("email", out var to) || string.IsNullOrWhiteSpace(to))
        {
            return;
        }

        await _emailSender.SendAsync(new EmailMessage(to, message.Title, message.Body, IsHtml: false), cancellationToken);
    }
}
