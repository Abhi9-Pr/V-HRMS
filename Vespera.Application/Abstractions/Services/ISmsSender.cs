namespace Vespera.Application.Abstractions.Services;

public sealed record SmsMessage(string ToPhoneNumber, string Body);

public interface ISmsSender
{
    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken);
}
