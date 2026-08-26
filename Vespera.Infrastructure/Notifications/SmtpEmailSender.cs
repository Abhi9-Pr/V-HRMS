using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Notifications;

/// <summary>The first real <see cref="IEmailSender"/> implementation — until now the port existed
/// with nothing behind it. Uses the BCL's <see cref="SmtpClient"/> rather than a third-party
/// package: this is a plain send-and-forget SMTP relay call, not worth a new dependency for.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
        };

        if (!string.IsNullOrEmpty(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml,
        };
        mailMessage.To.Add(message.To);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
