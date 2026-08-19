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
            return _cachedConnectionString;
        }
        catch (Exception ex) when (provisioner is DockerContainerProvisioner && _fallback is not null)
        {
            LogDockerFallthrough(_logger, ex);
            _cachedConnectionString = await _fallback.ProvisionAsync(cancellationToken);
            return _cachedConnectionString;
        }
    }
}
