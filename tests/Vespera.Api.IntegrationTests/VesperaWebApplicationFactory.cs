using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Authorization;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Identity;
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

    // Same isolation, for whichever provisioner Auto mode actually picks: on any machine/runner
    // where Docker is reachable (true on GitHub-hosted runners by default), DatabaseProvisionerSelector
    // picks Docker over the SQLite fallback — and without this, every factory instance shared one
    // "postgres" database on the one reused dev container (see DockerContainerProvisioner), which is
    // what was actually behind the intermittent attendance/payroll test failures this was chasing.
    private readonly string _dockerDatabaseName = $"integration_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Vespera:Database:Fallback:DatabaseFileName"] = _databaseFileName,
                ["Vespera:Database:Docker:DatabaseName"] = _dockerDatabaseName,
            }));

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

    /// <summary>Creates a brand-new second tenant with one Admin-equivalent user (every
    /// permission except Finance.Admin — the same shape as DevelopmentSeeder's own admin role)
    /// and logs in as them. For <c>CrossTenantIsolationTests</c>: a cross-tenant request made by a
    /// user with equivalent privileges to the demo tenant's admin means a 404 response can only be
    /// explained by tenant isolation, never by the caller simply lacking the permission for that
    /// action.</summary>
    public async Task<HttpClient> CreateSecondTenantAdminClientAsync()
    {
        var email = $"idor-admin-{Guid.NewGuid():N}@vespera.test";
        Guid tenantId;

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
            var credentialStore = scope.ServiceProvider.GetRequiredService<IUserCredentialStore>();

            var now = DateTimeOffset.UtcNow;
            const string createdBy = "test";

            var tenant = Tenant.Create("IDOR Second Tenant", $"IDOR-{Guid.NewGuid():N}"[..12], now, createdBy).Value;
            tenant.Reactivate(now, createdBy);
            dbContext.Add(tenant);
            var tid = tenant.Id;

            var allPermissions = await dbContext.Set<Permission>().AsNoTracking().ToListAsync();
            var role = Role.Create(tid, "IdorAdmin", now, createdBy).Value;
            foreach (var permission in allPermissions.Where(p => p.Code != Permissions.Finance.Admin))
            {
                role.Grant(permission.Id, now, createdBy);
            }

            dbContext.Add(role);

            var user = User.Create(tid, EmailAddress.Create(email).Value, null, now, createdBy);
            user.AssignRole(role.Id, now, createdBy);
            dbContext.Add(user);

            // Not yet in the database — TransactionBehavior-style, nothing commits until the
            // SaveChangesAsync below (same as DevelopmentSeeder's own credential staging).
            await credentialStore.CreateAsync(user.Id, email, DevelopmentSeeder.DemoPassword, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            tenantId = tid.Value;
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = DevelopmentSeeder.DemoPassword, deviceId = "idor-test-device" });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login did not return a body.");

        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    /// <summary>Same shape as <see cref="CreateSecondTenantAdminClientAsync"/> but scoped to
    /// exactly the permission set DevelopmentSeeder grants its own "Finance" role (vikram/fatima),
    /// and TOTP-enrolled with the same fixed <see cref="DevelopmentSeeder.FinanceAdminTotpSecretBase32"/>
    /// secret so login can compute a valid code via <see cref="TotpTestHelper"/>. For finance-wall
    /// IDOR tests: the second-tenant caller must genuinely hold Finance.Admin (in a different
    /// tenant) for a 404 there to prove tenant isolation rather than a missing permission.</summary>
    public async Task<HttpClient> CreateSecondTenantFinanceAdminClientAsync()
    {
        var email = $"idor-finance-admin-{Guid.NewGuid():N}@vespera.test";
        Guid tenantId;

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
            var credentialStore = scope.ServiceProvider.GetRequiredService<IUserCredentialStore>();

            var now = DateTimeOffset.UtcNow;
            const string createdBy = "test";

            var tenant = Tenant.Create("IDOR Second Finance Tenant", $"IDORFIN-{Guid.NewGuid():N}", now, createdBy).Value;
            tenant.Reactivate(now, createdBy);
            dbContext.Add(tenant);
            var tid = tenant.Id;

            var financePermissionCodes = new[]
            {
                Permissions.Finance.Admin,
                Permissions.Payroll.Read,
                Permissions.Payroll.Write,
                Permissions.Payroll.Finalize,
                Permissions.Expenses.ManagePolicy,
                Permissions.Expenses.Settle,
                Permissions.Recruitment.ApproveRequisitions,
                Permissions.Workspace.ViewDashboard,
            };
            // Filtered in-memory (not via a translated .Where(p => financePermissionCodes
            // .Contains(p.Code)) query) — that shape crashes EF's expression-tree interpreter on
            // this array (ReadOnlySpan/generic-constraint failure inside
            // ParameterExtractingExpressionVisitor), unrelated to the query's own logic.
            var allPermissions = await dbContext.Set<Permission>().AsNoTracking().ToListAsync();
            var permissions = allPermissions.Where(p => financePermissionCodes.Contains(p.Code)).ToList();

            var role = Role.Create(tid, "IdorFinanceAdmin", now, createdBy).Value;
            foreach (var permission in permissions)
            {
                role.Grant(permission.Id, now, createdBy);
            }

            dbContext.Add(role);

            var user = User.Create(tid, EmailAddress.Create(email).Value, null, now, createdBy);
            user.AssignRole(role.Id, now, createdBy);
            dbContext.Add(user);

            await credentialStore.CreateAsync(user.Id, email, DevelopmentSeeder.DemoPassword, CancellationToken.None);

            // Same pattern as DevelopmentSeeder's own vikram/fatima TOTP enrollment: the
            // ApplicationUser row credentialStore.CreateAsync just staged isn't in the database
            // yet, so this reads the change tracker's local set rather than a query that would
            // find nothing.
            var applicationUser = dbContext.Set<ApplicationUser>().Local.Single(u => u.Id == user.Id.Value);
            applicationUser.AuthenticatorKey = DevelopmentSeeder.FinanceAdminTotpSecretBase32;
            applicationUser.TwoFactorEnabled = true;

            await dbContext.SaveChangesAsync(CancellationToken.None);

            tenantId = tid.Value;
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = DevelopmentSeeder.DemoPassword, deviceId = "idor-finance-test-device", totpCode = code });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login did not return a body.");

        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
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
