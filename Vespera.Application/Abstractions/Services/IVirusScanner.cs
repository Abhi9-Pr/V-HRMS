namespace Vespera.Application.Abstractions.Services;

public enum ScanResult
{
    Clean,
    Infected,
    ScanFailed,
}

public interface IVirusScanner
{
    public Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken);
}
