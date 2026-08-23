namespace Vespera.Infrastructure.Security;

public sealed class VirusScanOptions
{
    public const string SectionName = "Vespera:VirusScan";

    /// <summary>"ClamAv" or "Null" (always-clean, local dev only).</summary>
    public string Provider { get; set; } = "Null";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 3310;

    public int TimeoutSeconds { get; set; } = 30;
}
