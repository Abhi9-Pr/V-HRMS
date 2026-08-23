namespace Vespera.Infrastructure.Notifications;

public sealed class SmtpOptions
{
    public const string SectionName = "Vespera:Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 25;

    public bool EnableSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@vespera.test";

    public string FromDisplayName { get; set; } = "Vespera HRMS";
}
