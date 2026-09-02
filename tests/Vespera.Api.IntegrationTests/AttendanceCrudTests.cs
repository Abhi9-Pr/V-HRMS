using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Api;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the web punch capture slice (M3) works end to end against the real pipeline:
/// a punch inside the assigned geofence succeeds, one outside is rejected with a specific
/// human-readable reason, and the optional IP allowlist rejects a non-matching caller.</summary>
public class AttendanceCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Matches DevelopmentSeeder's "Head Office" location exactly.
    private const double HeadOfficeLatitude = 12.9716;
    private const double HeadOfficeLongitude = 77.5946;

    private readonly VesperaWebApplicationFactory _factory;

    public AttendanceCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SeedGeofenceZoneAsync(double centerLatitude, double centerLongitude, double radiusMetres)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var headOffice = await dbContext.Set<Location>().IgnoreQueryFilters().FirstAsync(l => l.Name == "Head Office" && l.TenantId == tenantId);

        var zone = GeofenceZone.Create(
            tenantId, headOffice.Id, "Test Fence", GeoCoordinate.Create(centerLatitude, centerLongitude).Value,
            radiusMetres, DateTimeOffset.UtcNow, "test").Value;
        dbContext.Add(zone);
        await dbContext.SaveChangesAsync();
        return zone.Id.Value;
    }

    private static async Task<Guid> GetPriyaEmployeeIdAsync(HttpClient hrClient)
    {
        var listResponse = await hrClient.GetAsync("/api/v1/employees?page=1&pageSize=50");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult>(JsonOptions);
        var priya = page!.Items.Single(e => e.Code == "EMP-001");
        return priya.Id;
    }

    [Fact]
    public async Task Punch_Inside_The_Geofence_Should_Succeed_And_Outside_Should_Be_Rejected_With_A_Reason()
    {
        await SeedGeofenceZoneAsync(HeadOfficeLatitude, HeadOfficeLongitude, radiusMetres: 200);

        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-attendance-crud");
        var priyaEmployeeId = await GetPriyaEmployeeIdAsync(hrClient);

        var (priyaClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-attendance-crud");

        // A few metres from the fence centre — well inside a 200m radius.
        var inResponse = await priyaClient.PostAsJsonAsync("/api/v1/attendance/punch", new
        {
            employeeId = priyaEmployeeId, punchType = "In", latitude = HeadOfficeLatitude + 0.0002, longitude = HeadOfficeLongitude + 0.0002,
        });
        var inBody = await inResponse.Content.ReadAsStringAsync();
        inResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, inBody);

        // Roughly 11km away — far outside a 200m radius. Also the alternating-punch "Out" type,
        // so this exercises the geofence rejection specifically, not the same-punch-type guard.
        var outResponse = await priyaClient.PostAsJsonAsync("/api/v1/attendance/punch", new
        {
            employeeId = priyaEmployeeId, punchType = "Out", latitude = HeadOfficeLatitude + 0.1, longitude = HeadOfficeLongitude,
        });
        var problemBody = await outResponse.Content.ReadAsStringAsync();
        outResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, problemBody);
        problemBody.Should().Contain("attendance.punch.outside_geofence");
        problemBody.Should().MatchRegex(@"\d+m"); // the message reports real distance/radius numbers
    }

    [Fact]
    public async Task Punch_Should_Be_Rejected_When_The_Callers_Network_Is_Not_Allowlisted()
    {
        // WithWebHostBuilder returns the base WebApplicationFactory<Program> type, not
        // VesperaWebApplicationFactory, so the login/tenant-header helpers below are inlined
        // rather than reused — this factory's own seed data (fresh SQLite file) is otherwise
        // identical to the class fixture's.
        await using var restrictedFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Vespera:WebPunch:AllowlistEnabled"] = "true",
                    ["Vespera:WebPunch:Cidrs:0"] = "203.0.113.0/24", // deliberately never matches the TestServer's loopback caller
                })));

        using var scope = restrictedFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenantId = (await dbContext.Set<Tenant>().FirstAsync(t => t.Code == DevelopmentSeeder.DemoTenantCode)).Id.Value;

        var hrClient = await LoginAsync(restrictedFactory, tenantId, "rohan.verma@demo.vespera.test");
        var priyaEmployeeId = await GetPriyaEmployeeIdAsync(hrClient);
        var priyaClient = await LoginAsync(restrictedFactory, tenantId, "priya.sharma@demo.vespera.test");

        var response = await priyaClient.PostAsJsonAsync("/api/v1/attendance/punch", new
        {
            employeeId = priyaEmployeeId, punchType = "In", latitude = (double?)null, longitude = (double?)null,
        });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        body.Should().Contain("attendance.punch.ip_not_allowed");
    }

    [Fact]
    public async Task Punch_For_An_Employee_Owned_By_Another_Tenant_Should_Not_Succeed()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-attendance-idor");
        var priyaEmployeeId = await GetPriyaEmployeeIdAsync(hrClient);

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        var response = await otherTenantClient.PostAsJsonAsync("/api/v1/attendance/punch", new
        {
            employeeId = priyaEmployeeId, punchType = "In", latitude = HeadOfficeLatitude, longitude = HeadOfficeLongitude,
        });

        response.StatusCode.Should().NotBe(HttpStatusCode.NoContent, "an employee id from another tenant must never be a valid punch target");
    }

    private static async Task<HttpClient> LoginAsync(WebApplicationFactory<Program> factory, Guid tenantId, string email)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = DevelopmentSeeder.DemoPassword, deviceId = $"device-{Guid.NewGuid():N}", totpCode = (string?)null });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>() ?? throw new InvalidOperationException("Login did not return a body.");

        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private sealed record PagedResult(IReadOnlyList<EmployeeSummary> Items);

    private sealed record EmployeeSummary(Guid Id, string Code);
}
