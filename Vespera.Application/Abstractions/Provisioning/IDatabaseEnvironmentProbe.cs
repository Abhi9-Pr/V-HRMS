namespace Vespera.Application.Abstractions.Provisioning;

public enum DatabaseEnvironmentStatus
{
    NotProvisioned,
    Provisioning,
    Ready,
    Faulted,
}

public sealed record DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus Status, string Reason);

public interface IDatabaseEnvironmentProbe
{
    public Task<DatabaseEnvironmentProbeResult> ProbeAsync(CancellationToken cancellationToken);
}
