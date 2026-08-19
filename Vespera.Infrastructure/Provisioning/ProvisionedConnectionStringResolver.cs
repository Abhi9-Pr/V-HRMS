using Microsoft.Extensions.Logging;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Provisioners;

namespace Vespera.Infrastructure.Provisioning;

/// <summary>
/// Selects a provisioner, runs it, and caches the result for the app's lifetime. If the
/// selected strategy is Docker and it fails during actual provisioning (as opposed to at the
/// probe stage), drops to the SQLite fallback when one is configured rather than propagating —
/// this is the "on failure, fall through" behavior for DockerContainerProvisioner.
/// </summary>
public sealed class ProvisionedConnectionStringResolver : IConnectionStringResolver
{
    private static readonly Action<ILogger, Exception> LogDockerFallthrough = LoggerMessage.Define(
        LogLevel.Warning, new EventId(1, nameof(LogDockerFallthrough)), "Docker provisioning failed; falling through to the SQLite fallback");

    private readonly IDatabaseProvisionerSelector _selector;
    private readonly SqliteFallbackProvisioner? _fallback;
    private readonly ILogger<ProvisionedConnectionStringResolver> _logger;

    private string? _cachedConnectionString;

    /// <summary>True once <see cref="ResolveAsync"/> has fallen through to <see cref="SqliteFallbackProvisioner"/>,
    /// whether because Docker provisioning failed or because Auto mode's own candidate chain reached it directly.
    /// Read by <c>AddVesperaPersistence</c> to pick the matching EF provider/migrations set.</summary>
    public bool UsedSqliteFallback { get; private set; }

    public ProvisionedConnectionStringResolver(
        IDatabaseProvisionerSelector selector, ILogger<ProvisionedConnectionStringResolver> logger, SqliteFallbackProvisioner? fallback = null)
    {
        _selector = selector;
        _logger = logger;
        _fallback = fallback;
    }

    public async Task<string> ResolveAsync(CancellationToken cancellationToken)
    {
        if (_cachedConnectionString is not null)
        {
            return _cachedConnectionString;
        }

        var provisioner = await _selector.SelectAsync(cancellationToken);

        try
        {
            _cachedConnectionString = await provisioner.ProvisionAsync(cancellationToken);
            UsedSqliteFallback = provisioner is SqliteFallbackProvisioner;
            return _cachedConnectionString;
        }
        catch (Exception ex) when (provisioner is DockerContainerProvisioner && _fallback is not null)
        {
            LogDockerFallthrough(_logger, ex);
            _cachedConnectionString = await _fallback.ProvisionAsync(cancellationToken);
            UsedSqliteFallback = true;
            return _cachedConnectionString;
        }
    }
}
