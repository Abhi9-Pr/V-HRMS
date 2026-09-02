using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the biometric device registration + quarantine-resolution slice (Attendance
/// M7) works end to end against the real pipeline. The poller itself isn't a background hosted
/// service under "IntegrationTesting" (see VesperaWebApplicationFactory), so a quarantined entry
/// is seeded directly via the DbContext — the same technique RegularizationsCrudTests uses for
/// setup that doesn't need to prove its own HTTP surface — while resolution itself goes through
/// the real HTTP endpoint.</summary>
public class BiometricDevicesCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public BiometricDevicesCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_And_List_Should_Round_Trip_For_A_User_With_BiometricDevices_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-biometric-devices-crud");

        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "Device Test Site", addressLine = "3 Sensor St", city = "Bengaluru", country = "India", latitude = 12.9716, longitude = 77.5946, timeZoneId = "Asia/Kolkata" });
        locationResponse.EnsureSuccessStatusCode();
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>(JsonOptions);

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/biometric-devices",
            new { locationId = location!.Id, vendorType = "ZKTeco", host = "device.local", port = 4370, apiKeyConfigurationKey = (string?)null });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registered = await registerResponse.Content.ReadFromJsonAsync<CreatedResponse>(JsonOptions);

        var listResponse = await client.GetAsync("/api/v1/biometric-devices?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<BiometricDeviceResponse>>(JsonOptions);
        page!.Items.Should().Contain(d => d.Id == registered!.Id && d.Host == "device.local");
    }

    [Fact]
    public async Task Register_Should_Return_Forbidden_For_A_User_Without_BiometricDevices_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-biometric-devices-forbidden");

        var response = await client.PostAsJsonAsync(
            "/api/v1/biometric-devices",
            new { locationId = Guid.NewGuid(), vendorType = "ZKTeco", host = "device.local", port = 4370, apiKeyConfigurationKey = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Resolving_A_Quarantined_Punch_Should_Create_A_Real_AttendancePunch_For_The_Given_Employee()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-biometric-quarantine-resolve");

        var employeesResponse = await client.GetAsync("/api/v1/employees?page=1&pageSize=50");
        var employeePage = await employeesResponse.Content.ReadFromJsonAsync<PagedResult<EmployeeResponse>>(JsonOptions);
        var priyaEmployeeId = employeePage!.Items.Single(e => e.Code == "EMP-001").Id;

        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        Guid quarantineId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
            var device = BiometricDevice.Register(
                tenantId, Domain.Eis.LocationId.New(), BiometricVendorType.ZKTeco, "device.local", 4370, null,
                DateTimeOffset.UtcNow, "seed").Value;
            var entry = QuarantinedBiometricPunch.Create(
                tenantId, device.Id, "ZK-UNMATCHED", DateTimeOffset.UtcNow, PunchType.In, "rec-integration-1");
            dbContext.Add(device);
            dbContext.Add(entry);
            await dbContext.SaveChangesAsync();
            quarantineId = entry.Id.Value;
        }

        var resolveResponse = await client.PostAsJsonAsync(
            $"/api/v1/biometric-devices/quarantined-punches/{quarantineId}/resolve", new { employeeId = priyaEmployeeId });
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var quarantineRow = await verifyDbContext.Set<QuarantinedBiometricPunch>().IgnoreQueryFilters()
            .SingleAsync(q => q.Id == new QuarantinedBiometricPunchId(quarantineId));
        quarantineRow.Status.Should().Be(QuarantinedBiometricPunchStatus.Resolved);

        var days = await verifyDbContext.Set<AttendanceDay>().IgnoreQueryFilters()
            .Where(d => d.TenantId == tenantId && d.EmployeeId == new Domain.Eis.EmployeeId(priyaEmployeeId))
            .ToListAsync();
        days.Should().Contain(d => d.Punches.Any(p => p.Source == PunchSource.Biometric));
    }

    [Fact]
    public async Task Resolving_A_Quarantined_Punch_Owned_By_Another_Tenant_Should_Return_NotFound()
    {
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        Guid quarantineId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
            var device = BiometricDevice.Register(
                tenantId, Domain.Eis.LocationId.New(), BiometricVendorType.ZKTeco, "device.local", 4370, null,
                DateTimeOffset.UtcNow, "seed").Value;
            var entry = QuarantinedBiometricPunch.Create(
                tenantId, device.Id, "ZK-UNMATCHED-IDOR", DateTimeOffset.UtcNow, PunchType.In, "rec-integration-idor");
            dbContext.Add(device);
            dbContext.Add(entry);
            await dbContext.SaveChangesAsync();
            quarantineId = entry.Id.Value;
        }

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        var resolveResponse = await otherTenantClient.PostAsJsonAsync(
            $"/api/v1/biometric-devices/quarantined-punches/{quarantineId}/resolve", new { employeeId = Guid.NewGuid() });
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record BiometricDeviceResponse(Guid Id, string Host);

    private sealed record EmployeeResponse(Guid Id, string Code);

    private sealed record PagedResult<T>(IReadOnlyList<T> Items);
}
