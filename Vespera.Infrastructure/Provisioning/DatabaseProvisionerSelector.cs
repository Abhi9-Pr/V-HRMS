using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vespera.Application.Abstractions.Provisioning;

namespace Vespera.Infrastructure.Provisioning;

public sealed class DatabaseProvisionerSelector : IDatabaseProvisionerSelector
{
    private static readonly Action<ILogger, string, string, Exception?> LogProbeResult = LoggerMessage.Define<string, string>(
        LogLevel.Information, new EventId(1, nameof(LogProbeResult)), "{Name}: {Reason}");

    private static readonly Action<ILogger, string, Exception?> LogUsingStrategy = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(2, nameof(LogUsingStrategy)), "Using {Name}");

    private readonly IReadOnlyList<DatabaseProvisioningCandidate> _candidates;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DatabaseProvisionerSelector> _logger;

    public DatabaseProvisionerSelector(
        IEnumerable<DatabaseProvisioningCandidate> candidates, IHostEnvironment environment, ILogger<DatabaseProvisionerSelector> logger)
    {
        _candidates = candidates.ToList();
        _environment = environment;
        _logger = logger;
    }

    public async Task<IDatabaseProvisioner> SelectAsync(CancellationToken cancellationToken)
    {
        foreach (var candidate in _candidates)
        {
            var result = await candidate.Probe.ProbeAsync(cancellationToken);
            LogProbeResult(_logger, candidate.Name, result.Reason, null);

            if (result.Status != DatabaseEnvironmentStatus.Ready)
            {
                continue;
            }

            if (_environment.IsProduction() && candidate.Kind != DatabaseStrategyKind.External)
            {
                throw new InvalidOperationException(
                    $"Refusing to use the '{candidate.Name}' database strategy in Production; only External is permitted.");
            }

            LogUsingStrategy(_logger, candidate.Name, null);
            return candidate.Provisioner;
        }

        throw new InvalidOperationException(
            "No database provisioning strategy could be resolved. Checked: " + string.Join(", ", _candidates.Select(c => c.Name)) + ".");
    }
}
