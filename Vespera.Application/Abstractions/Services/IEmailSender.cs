namespace Vespera.Application.Abstractions.Services;

public sealed record EmailMessage(string To, string Subject, string Body, bool IsHtml);

public interface IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
