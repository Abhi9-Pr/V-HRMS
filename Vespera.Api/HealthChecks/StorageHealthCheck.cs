using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Api.HealthChecks;

/// <summary>Writes and deletes a small probe file through the configured IFileStorage.</summary>
public sealed class StorageHealthCheck : IHealthCheck
{
    private readonly IFileStorage _fileStorage;

    public StorageHealthCheck(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var content = new MemoryStream(Encoding.UTF8.GetBytes("health-check-probe"));
            var storageKey = await _fileStorage.UploadAsync("health-check-probe.txt", content, cancellationToken);
            await _fileStorage.DeleteAsync(storageKey, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Storage probe failed.", ex);
        }
    }
}
