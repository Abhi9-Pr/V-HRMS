using Vespera.Application.Abstractions.Provisioning;

namespace Vespera.Infrastructure.Provisioning.Probes;

/// <summary>SQLite fallback is a local file — there is nothing to probe, so this is always Ready.</summary>
public sealed class SqliteFallbackProbe : IDatabaseEnvironmentProbe
{
    public Task<DatabaseEnvironmentProbeResult> ProbeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.Ready, "SQLite fallback is always available"));
}
