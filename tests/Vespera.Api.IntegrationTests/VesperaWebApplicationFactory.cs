using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.IdentityAccess;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>
/// The "IntegrationTesting" environment (distinct from "Testing", which stays entirely DB-free
/// for the trivial health-check test): a real, isolated SQLite database per factory instance
/// (via VesperaDatabaseOptions.Fallback.DatabaseFileName — see SqliteFallbackProvisioner), schema
/// applied and DevelopmentSeeder run automatically at host startup
/// (DatabaseServiceCollectionExtensions.InitializeVesperaDatabaseAsync), background hosted
/// services not registered (see Program.cs) so tests control outbox/retention timing themselves.
/// </summary>
public sealed class VesperaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseFileName = $"integration-test-{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Vespera:Database:Fallback:DatabaseFileName"] = _databaseFileName }));

        // Neither real OCR adapter (TesseractDocumentOcrService needs native binaries + a
        // .traineddata file on disk; AzureDocumentIntelligenceOcrService needs a real Azure
        // endpoint) can run in CI — swap in a canned fake so tests can exercise the onboarding
        // OCR-extract/confirm flow over real HTTP without either dependency.
        builder.ConfigureTestServices(services =>
            services.AddScoped<IDocumentOcrService, FakeDocumentOcrService>());
    }

    public async Task<Guid> GetDemoTenantIdAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenant = await dbContext.Set<Tenant>().FirstAsync(t => t.Code == DevelopmentSeeder.DemoTenantCode);
        return tenant.Id.Value;
    }

    /// <summary>A client with the demo tenant header attached but no bearer token yet — for
    /// exercising login/refresh/register/forgot-password themselves.</summary>
    public async Task<HttpClient> CreateTenantScopedClientAsync()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", (await GetDemoTenantIdAsync()).ToString());
        return client;
    }

    /// <summary>Logs in as a seeded demo user via the real /api/v1/auth/login endpoint and
    /// returns a client with the resulting bearer token attached — and the header removed, so
    /// every subsequent request genuinely exercises the JWT-only authenticated path a real
    /// client uses post-login (see HttpTenantContext's own doc comment: the header is a
    /// pre-auth-only fallback, and leaving it on every request here would silently mask a bug in
    /// that JWT-only path, which it once did).</summary>
    public async Task<(HttpClient Client, LoginResponse Login)> CreateAuthenticatedClientAsync(
        string email, string password, string deviceId, string? totpCode = null)
    {
        var client = await CreateTenantScopedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password, deviceId, totpCode });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login did not return a body.");

        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return (client, login);
    }

    protected override void Dispose(bool disposing)
    {
        string? contentRoot = null;
        if (disposing)
        {
            try
            {
                contentRoot = Services.GetService<IHostEnvironment>()?.ContentRootPath;
            }
            catch
            {
                // Host may already be torn down; best-effort cleanup only.
            }
        }

        base.Dispose(disposing);

        if (!disposing || contentRoot is null)
        {
            return;
        }

        foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
        {
            try
            {
                File.Delete(Path.Combine(contentRoot, ".vespera", _databaseFileName + suffix));
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }
}

public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

/// <summary>Canned <see cref="IDocumentOcrService"/> for the IntegrationTesting environment —
/// see <see cref="VesperaWebApplicationFactory.ConfigureWebHost"/>.</summary>
public sealed class FakeDocumentOcrService : IDocumentOcrService
{
    public Task<OcrResult> ExtractAsync(Stream document, CancellationToken cancellationToken) =>
        Task.FromResult(new OcrResult(
            "FAKE OCR TEXT", new Dictionary<string, string> { ["firstName"] = "OcrSuggested" }, 0.9));
}
