using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vespera.Application.Abstractions.Provisioning;

namespace Vespera.Infrastructure.Provisioning.Provisioners;

/// <summary>Dev-only last resort. Never selectable in Production — see DatabaseProvisionerSelector.</summary>
public sealed class SqliteFallbackProvisioner : IDatabaseProvisioner
{
    private static readonly Action<ILogger, string, Exception?> LogFallbackInUse = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1, nameof(LogFallbackInUse)),
        "Vespera is running against a SQLite fallback database at {DatabasePath}. " +
        "This is for local development only and must never be used in Production.");

    private readonly IHostEnvironment _environment;
    private readonly ILogger<SqliteFallbackProvisioner> _logger;

    public SqliteFallbackProvisioner(IHostEnvironment environment, ILogger<SqliteFallbackProvisioner> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task<string> ProvisionAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_environment.ContentRootPath, ".vespera");
        Directory.CreateDirectory(directory);
        var databasePath = Path.Combine(directory, "dev.db");

        LogFallbackInUse(_logger, databasePath, null);

        return Task.FromResult($"Data Source={databasePath}");
    }
}
