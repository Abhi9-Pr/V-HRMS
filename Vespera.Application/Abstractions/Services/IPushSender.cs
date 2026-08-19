namespace Vespera.Application.Abstractions.Services;

public sealed record PushMessage(string DeviceToken, string Title, string Body);

public interface IPushSender
{
    public Task SendAsync(PushMessage message, CancellationToken cancellationToken);
}
