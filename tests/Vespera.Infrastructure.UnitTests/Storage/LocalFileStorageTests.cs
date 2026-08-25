using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vespera.Infrastructure.Storage;

namespace Vespera.Infrastructure.UnitTests.Storage;

public class LocalFileStorageTests : IDisposable
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Vespera.Infrastructure.UnitTests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private readonly string _tempRoot;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"vespera-storage-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);

        var environment = new FakeHostEnvironment { ContentRootPath = _tempRoot };
        var options = Options.Create(new LocalFileStorageOptions { SigningSecret = "test-signing-secret" });
        _storage = new LocalFileStorage(environment, options);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_Should_Produce_A_Signature_That_ValidateSignature_Accepts()
    {
        using var content = new MemoryStream("hello"u8.ToArray());
        var storageKey = await _storage.UploadAsync("id-proof.png", content, CancellationToken.None);

        var uri = await _storage.GetDownloadUrlAsync(storageKey, TimeSpan.FromMinutes(5), CancellationToken.None);
        var query = System.Web.HttpUtility.ParseQueryString(uri.OriginalString[(uri.OriginalString.IndexOf('?') + 1)..]);

        var expires = long.Parse(query["expires"]!);
        var signature = query["sig"]!;

        _storage.ValidateSignature(storageKey, expires, signature).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSignature_Should_Reject_A_Tampered_StorageKey()
    {
        using var content = new MemoryStream("hello"u8.ToArray());
        var storageKey = await _storage.UploadAsync("id-proof.png", content, CancellationToken.None);

        var uri = await _storage.GetDownloadUrlAsync(storageKey, TimeSpan.FromMinutes(5), CancellationToken.None);
        var query = System.Web.HttpUtility.ParseQueryString(uri.OriginalString[(uri.OriginalString.IndexOf('?') + 1)..]);

        var expires = long.Parse(query["expires"]!);
        var signature = query["sig"]!;

        _storage.ValidateSignature("some-other-key", expires, signature).Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSignature_Should_Reject_An_Expired_Url()
    {
        using var content = new MemoryStream("hello"u8.ToArray());
        var storageKey = await _storage.UploadAsync("id-proof.png", content, CancellationToken.None);

        var uri = await _storage.GetDownloadUrlAsync(storageKey, TimeSpan.FromSeconds(-1), CancellationToken.None);
        var query = System.Web.HttpUtility.ParseQueryString(uri.OriginalString[(uri.OriginalString.IndexOf('?') + 1)..]);

        var expires = long.Parse(query["expires"]!);
        var signature = query["sig"]!;

        _storage.ValidateSignature(storageKey, expires, signature).Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_Then_DownloadAsync_Should_Round_Trip_Content()
    {
        using var content = new MemoryStream("hello world"u8.ToArray());
        var storageKey = await _storage.UploadAsync("note.txt", content, CancellationToken.None);

        await using var downloaded = await _storage.DownloadAsync(storageKey, CancellationToken.None);
        using var reader = new StreamReader(downloaded);
        var text = await reader.ReadToEndAsync();

        text.Should().Be("hello world");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
