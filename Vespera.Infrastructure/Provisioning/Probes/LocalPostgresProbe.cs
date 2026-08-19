using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Npgsql;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.Provisioning.Probes;

public sealed class LocalPostgresProbe : IDatabaseEnvironmentProbe
{
    private static readonly TimeSpan TcpTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(3);

    private readonly IOptions<VesperaDatabaseOptions> _options;

    public LocalPostgresProbe(IOptions<VesperaDatabaseOptions> options)
    {
        _options = options;
    }

    public async Task<DatabaseEnvironmentProbeResult> ProbeAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var local = options.Local;

        if (options.Engine != DatabaseEngine.Postgres)
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.NotProvisioned, "configured engine is not Postgres");
        }

        if (!await TryConnectTcpAsync(local.Host, local.Port, cancellationToken))
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.NotProvisioned, $"no listener on {local.Host}:{local.Port}");
        }

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = local.Host,
            Port = local.Port,
            Username = local.Username,
            Password = local.Password,
            Database = "postgres",
            Timeout = (int)HandshakeTimeout.TotalSeconds,
        }.ConnectionString;

        try
        {
            using var timeoutCts = new CancellationTokenSource(HandshakeTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await DatabaseHandshake.PingAsync(DatabaseEngine.Postgres, connectionString, linkedCts.Token);
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.Ready, $"Postgres reachable at {local.Host}:{local.Port}");
        }
        catch (Exception ex)
        {
            return new DatabaseEnvironmentProbeResult(
                DatabaseEnvironmentStatus.Faulted, $"listener present at {local.Host}:{local.Port} but handshake failed: {ex.Message}");
        }
    }

    private static async Task<bool> TryConnectTcpAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TcpTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, linkedCts.Token);
            return true;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return false;
        }
    }
}
