using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.Provisioning.Probes;

public sealed class ExternalConnectionProbe : IDatabaseEnvironmentProbe
{
    private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(5);

    private readonly IConfiguration _configuration;
    private readonly IOptions<VesperaDatabaseOptions> _options;

    public ExternalConnectionProbe(IConfiguration configuration, IOptions<VesperaDatabaseOptions> options)
    {
        _configuration = configuration;
        _options = options;
    }

    public async Task<DatabaseEnvironmentProbeResult> ProbeAsync(CancellationToken cancellationToken)
    {
        var connectionStringName = _options.Value.External.ConnectionStringName;

        if (string.IsNullOrWhiteSpace(connectionStringName))
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.NotProvisioned, "no External:ConnectionStringName configured");
        }

        var connectionString = _configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.NotProvisioned, $"ConnectionStrings:{connectionStringName} is not set");
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(HandshakeTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await DatabaseHandshake.PingAsync(_options.Value.Engine, connectionString, linkedCts.Token);
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.Ready, $"external connection '{connectionStringName}' opened and SELECT 1 succeeded");
        }
        catch (Exception ex)
        {
            return new DatabaseEnvironmentProbeResult(DatabaseEnvironmentStatus.Faulted, $"external connection '{connectionStringName}' failed: {ex.Message}");
        }
    }
}
