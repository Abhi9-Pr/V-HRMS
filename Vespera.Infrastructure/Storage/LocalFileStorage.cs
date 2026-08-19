using Microsoft.Extensions.Hosting;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Storage;

/// <summary>
/// Minimal local-disk <see cref="IFileStorage"/> — no implementation existed before Phase 4
/// (like <c>IPiiProtector</c> before Phase 3). Dev-appropriate, same spirit as the SQLite
/// database fallback: good enough to exercise the port and give the storage health check
/// something real to probe; a production object-store provider is a later-phase concern.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IHostEnvironment environment)
    {
        _root = Path.Combine(environment.ContentRootPath, ".vespera-storage");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        var storageKey = $"{Guid.NewGuid():N}-{fileName}";
        await using var fileStream = File.Create(Path.Combine(_root, storageKey));
        await content.CopyToAsync(fileStream, cancellationToken);
        return storageKey;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(File.OpenRead(Path.Combine(_root, storageKey)));

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        File.Delete(Path.Combine(_root, storageKey));
        return Task.CompletedTask;
    }
}
