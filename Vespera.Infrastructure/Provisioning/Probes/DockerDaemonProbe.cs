using Docker.DotNet;
using Vespera.Application.Abstractions.Provisioning;

namespace Vespera.Infrastructure.Provisioning.Probes;

/// <summary>
/// Verifies the Docker DAEMON is reachable via a real API ping over the daemon socket/named
/// pipe — `docker --version` is not a valid substitute, since it succeeds even when the daemon
/// itself is stopped.
/// </summary>
public sealed class DockerDaemonProbe : IDatabaseEnvironmentProbe
{
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(3);

    public async Task<DatabaseEnvironmentProbeResult> ProbeAsync(CancellationToken cancellationToken)
    {
        using var client = new DockerClientConfiguration().CreateClient();
        using var timeoutCts = new CancellationTokenSource(PingTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await client.System.PingAsync(linkedCts.Token);
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.Ready, "Docker daemon reachable");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DatabaseEnvironmentProbeResult(
                DatabaseEnvironmentStatus.NotProvisioned,
                $"Docker daemon did not respond within {PingTimeout.TotalSeconds:0}s (not installed, stopped, or Docker Desktop still starting)");
        }
        catch (Exception ex)
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.NotProvisioned, $"Docker daemon unreachable: {ex.Message}");
        }
    }
}
