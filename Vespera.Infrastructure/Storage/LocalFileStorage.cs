using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    private readonly LocalFileStorageOptions _options;

    public LocalFileStorage(IHostEnvironment environment, IOptions<LocalFileStorageOptions> options)
    {
        _root = Path.Combine(environment.ContentRootPath, ".vespera-storage");
        Directory.CreateDirectory(_root);
        _options = options.Value;
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

    /// <summary>
    /// Local disk has no native pre-signed-URL concept, so this signs its own callback URL — an
    /// HMAC-SHA256 over <c>(storageKey, expiresUnixSeconds)</c> that
    /// <c>Vespera.Api.Controllers.V1.FilesController</c> verifies before streaming. A future
    /// S3/Blob adapter would return that provider's own pre-signed URL instead of this scheme —
    /// same port, no caller changes.
    /// </summary>
    public Task<Uri> GetDownloadUrlAsync(string storageKey, TimeSpan expiry, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        var signature = ComputeSignature(storageKey, expiresAt);
        var uri = new Uri(
            $"/api/v1/files/{Uri.EscapeDataString(storageKey)}/download?expires={expiresAt}&sig={Uri.EscapeDataString(signature)}",
            UriKind.Relative);

        return Task.FromResult(uri);
    }

    public bool ValidateSignature(string storageKey, long expiresAtUnixSeconds, string signature)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAtUnixSeconds)
        {
            return false;
        }

        var expected = ComputeSignature(storageKey, expiresAtUnixSeconds);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(signature);

        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private string ComputeSignature(string storageKey, long expiresAtUnixSeconds)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningSecret));
        var payload = Encoding.UTF8.GetBytes($"{storageKey}:{expiresAtUnixSeconds}");
        return Convert.ToHexString(hmac.ComputeHash(payload));
    }
}
