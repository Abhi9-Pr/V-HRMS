namespace Vespera.Application.Abstractions.Services;

public interface IFileStorage
{
    public Task<string> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken);

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken);

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>
    /// A time-limited download URL for <paramref name="storageKey"/>. A disk-backed provider
    /// signs its own callback URL (see <c>LocalFileStorage</c>); an object-store provider (S3,
    /// Azure Blob) would return that provider's native pre-signed URL instead — callers never
    /// know which.
    /// </summary>
    public Task<Uri> GetDownloadUrlAsync(string storageKey, TimeSpan expiry, CancellationToken cancellationToken);
}
