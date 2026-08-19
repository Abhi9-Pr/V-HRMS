namespace Vespera.Application.Abstractions.Services;

public interface IFileStorage
{
    public Task<string> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken);

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken);

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
