using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Security;

/// <summary>
/// Always reports <see cref="ScanResult.Clean"/> — local dev only, same spirit as
/// <c>LocalFileStorage</c>: good enough to exercise the port without a real clamd daemon running.
/// Selected via <c>VirusScanOptions.Provider = "Null"</c>.
/// </summary>
public sealed class NullVirusScanner : IVirusScanner
{
    public Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken) =>
        Task.FromResult(ScanResult.Clean);
}
