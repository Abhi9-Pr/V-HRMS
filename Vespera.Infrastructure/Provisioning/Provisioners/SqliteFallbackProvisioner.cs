using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;

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
    private readonly IOptions<VesperaDatabaseOptions> _options;
    private readonly ILogger<SqliteFallbackProvisioner> _logger;

    public SqliteFallbackProvisioner(IHostEnvironment environment, IOptions<VesperaDatabaseOptions> options, ILogger<SqliteFallbackProvisioner> logger)
    {
        _environment = environment;
        _options = options;
        _logger = logger;
    }

    public Task<string> ProvisionAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_environment.ContentRootPath, ".vespera");
        Directory.CreateDirectory(directory);
        var fileName = _options.Value.Fallback.DatabaseFileName ?? "dev.db";
        var databasePath = Path.Combine(directory, fileName);

        LogFallbackInUse(_logger, databasePath, null);

        return Task.FromResult($"Data Source={databasePath}");
    }
}
