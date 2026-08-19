using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.Provisioning.Probes;

public sealed class LocalSqlServerProbe : IDatabaseEnvironmentProbe
{
    private static readonly TimeSpan TcpTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(3);

    private readonly IOptions<VesperaDatabaseOptions> _options;

    public LocalSqlServerProbe(IOptions<VesperaDatabaseOptions> options)
    {
        _options = options;
    }

    public async Task<DatabaseEnvironmentProbeResult> ProbeAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var local = options.Local;

        if (options.Engine != DatabaseEngine.SqlServer)
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.NotProvisioned, "configured engine is not SqlServer");
        }

        if (!await TryConnectTcpAsync(local.Host, local.Port, cancellationToken))
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.NotProvisioned, $"no listener on {local.Host}:{local.Port}");
        }

        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = $"{local.Host},{local.Port}",
            UserID = local.Username,
            Password = local.Password,
            InitialCatalog = "master",
            ConnectTimeout = (int)HandshakeTimeout.TotalSeconds,
            TrustServerCertificate = true,
        }.ConnectionString;

        try
        {
            using var timeoutCts = new CancellationTokenSource(HandshakeTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await DatabaseHandshake.PingAsync(DatabaseEngine.SqlServer, connectionString, linkedCts.Token);
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.Ready, $"SQL Server reachable at {local.Host}:{local.Port}");
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
