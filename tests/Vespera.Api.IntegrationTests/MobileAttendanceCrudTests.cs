using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the mobile punch capture slice (M4) end to end — the direct backing for three
/// of the module's four "Done when" acceptance criteria: a punch inside the fence succeeds, one
/// outside is rejected with a specific reason, and a duplicate Idempotency-Key on this endpoint
/// returns the exact original response without recording a second punch.</summary>
public class MobileAttendanceCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Matches DevelopmentSeeder's "Head Office" location exactly.
    private const double HeadOfficeLatitude = 12.9716;
    private const double HeadOfficeLongitude = 77.5946;

    private readonly VesperaWebApplicationFactory _factory;

    public MobileAttendanceCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedGeofenceZoneAsync(double centerLatitude, double centerLongitude, double radiusMetres)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var headOffice = await dbContext.Set<Location>().IgnoreQueryFilters().FirstAsync(l => l.Name == "Head Office" && l.TenantId == tenantId);

        var existing = await dbContext.Set<GeofenceZone>().IgnoreQueryFilters()
            .Where(z => z.TenantId == tenantId && z.LocationId == headOffice.Id)
            .ToListAsync();
        if (existing.Count > 0)
        {
            return;
        }

        var zone = GeofenceZone.Create(
            tenantId, headOffice.Id, "Test Fence", GeoCoordinate.Create(centerLatitude, centerLongitude).Value,
            radiusMetres, DateTimeOffset.UtcNow, "test").Value;
        dbContext.Add(zone);
        await dbContext.SaveChangesAsync();
    }

    // Each test that actually persists a punch uses a distinct seeded employee (all located at
    // Head Office) — VesperaWebApplicationFactory is a shared IClassFixture, so a class with
    // several tests that each record a real "In" punch for the *same* employee on the same day
    // would trip AttendanceDay's alternating In/Out rule on whichever test runs second (xUnit
    // gives no ordering guarantee). Rejection-path tests (outside geofence, mock provider) never
    // persist anything, so they can safely share an employee.
    private static async Task<Guid> GetEmployeeIdByCodeAsync(HttpClient hrClient, string code)
    {
        var listResponse = await hrClient.GetAsync("/api/v1/employees?page=1&pageSize=50");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult>(JsonOptions);
        return page!.Items.Single(e => e.Code == code).Id;
    }

    private async Task<int> CountPersistedPunchesAsync(Guid employeeId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var days = await dbContext.Set<AttendanceDay>().IgnoreQueryFilters()
            .Where(d => d.TenantId == tenantId && d.EmployeeId == new EmployeeId(employeeId))
            .ToListAsync();
        return days.Sum(d => d.Punches.Count);
    }

    [Fact]
    public async Task Punch_Inside_The_Geofence_Should_Succeed()
    {
        await SeedGeofenceZoneAsync(HeadOfficeLatitude, HeadOfficeLongitude, radiusMetres: 200);

        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-mobile-attendance");
        var priyaEmployeeId = await GetEmployeeIdByCodeAsync(hrClient, "EMP-001");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/attendance/punch")
        {
            Content = JsonContent.Create(new
            {
                employeeId = priyaEmployeeId,
                punchType = "In",
                latitude = HeadOfficeLatitude + 0.0002,
                longitude = HeadOfficeLongitude + 0.0002,
                accuracy = 10.0,
                isFromMockProvider = false,
                deviceId = "priya-iphone",
            }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await hrClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task Punch_Outside_The_Geofence_Should_Be_Rejected_With_A_Specific_Reason()
    {
        await SeedGeofenceZoneAsync(HeadOfficeLatitude, HeadOfficeLongitude, radiusMetres: 200);

        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-mobile-attendance-2");
        var priyaEmployeeId = await GetEmployeeIdByCodeAsync(hrClient, "EMP-001");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/attendance/punch")
        {
            Content = JsonContent.Create(new
            {
                employeeId = priyaEmployeeId,
                punchType = "In",
                latitude = HeadOfficeLatitude + 0.1, // ~11km away — far outside a 200m radius
                longitude = HeadOfficeLongitude,
                accuracy = 10.0,
                isFromMockProvider = false,
                deviceId = "priya-iphone",
            }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await hrClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("attendance.punch.outside_geofence");
        body.Should().MatchRegex(@"\d+m");
    }

    [Fact]
    public async Task Punch_From_A_Mock_Provider_Should_Be_Rejected()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-mobile-attendance-3");
        var priyaEmployeeId = await GetEmployeeIdByCodeAsync(hrClient, "EMP-001");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/attendance/punch")
        {
            Content = JsonContent.Create(new
            {
                employeeId = priyaEmployeeId,
                punchType = "In",
                latitude = (double?)null,
                longitude = (double?)null,
                accuracy = 10.0,
                isFromMockProvider = true,
                deviceId = "priya-emulator",
            }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await hrClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("attendance.punch.mock_location_detected");
    }

    [Fact]
    public async Task Replaying_The_Same_Idempotency_Key_Should_Return_The_Original_Response_And_Persist_Only_One_Punch()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-mobile-attendance-4");
        var ananyaEmployeeId = await GetEmployeeIdByCodeAsync(hrClient, "EMP-003");

        var idempotencyKey = Guid.NewGuid().ToString();
        var punchesBefore = await CountPersistedPunchesAsync(ananyaEmployeeId);

        object PayloadFor() => new
        {
            employeeId = ananyaEmployeeId,
            punchType = "In",
            latitude = (double?)null,
            longitude = (double?)null,
            accuracy = 10.0,
            isFromMockProvider = false,
            deviceId = "ananya-android",
        };

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/attendance/punch") { Content = JsonContent.Create(PayloadFor()) };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await hrClient.SendAsync(firstRequest);
        var firstBody = await first.Content.ReadAsStringAsync();

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/attendance/punch") { Content = JsonContent.Create(PayloadFor()) };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var second = await hrClient.SendAsync(secondRequest);
        var secondBody = await second.Content.ReadAsStringAsync();

        first.StatusCode.Should().Be(HttpStatusCode.NoContent, firstBody);
        // If the handler had actually run a second time, the alternating In/Out domain rule would
        // reject the second "In" with a Conflict — a replayed response must be the identical 204
        // instead, byte-for-byte, per IdempotencyMiddleware.
        second.StatusCode.Should().Be(HttpStatusCode.NoContent, secondBody);
        secondBody.Should().Be(firstBody);

        var punchesAfter = await CountPersistedPunchesAsync(ananyaEmployeeId);
        (punchesAfter - punchesBefore).Should().Be(1);
    }

    private async Task<Guid> GetEmployeeIdByEmailAsync(string workEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        // Comparing the whole converted EmailAddress value object, not its nested .Value member —
        // the latter doesn't translate through the HasConversion mapping (same class of issue
        // noted elsewhere in this codebase for typed-id .Value access in a LINQ predicate).
        var email = EmailAddress.Create(workEmail).Value;
        var employee = await dbContext.Set<Employee>().IgnoreQueryFilters()
            .FirstAsync(e => e.TenantId == tenantId && e.WorkEmail == email);
        return employee.Id.Value;
    }

    [Fact]
    public async Task Sync_Should_Include_A_Punch_Recorded_Since_The_Given_Cursor()
    {
        // rohan.verma is used elsewhere in this fixture-shared class only as the HR *caller*
        // punching on behalf of other employees (priya/ananya) — he's never had a real punch
        // recorded against his own AttendanceDay, so self-punching for him here is safe (see the
        // class-level comment above CountPersistedPunchesAsync on why a distinct employee matters
        // per test). Finance-role demo users (fatima.khan/vikram.nair) require TOTP and can't
        // authenticate with just a password here, so they're not an option for this test.
        const string email = "rohan.verma@demo.vespera.test";
        var (employeeClient, _) = await _factory.CreateAuthenticatedClientAsync(
            email, DevelopmentSeeder.DemoPassword, "device-rohan-sync");
        var employeeId = await GetEmployeeIdByEmailAsync(email);
        var since = DateTimeOffset.UtcNow.AddMinutes(-5);

        using var punchRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/attendance/punch")
        {
            Content = JsonContent.Create(new
            {
                employeeId,
                punchType = "In",
                latitude = (double?)null,
                longitude = (double?)null,
                accuracy = 10.0,
                isFromMockProvider = false,
                deviceId = "fatima-sync-device",
            }),
        };
        punchRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var punchResponse = await employeeClient.SendAsync(punchRequest);
        var punchBody = await punchResponse.Content.ReadAsStringAsync();
        punchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, punchBody);

        var sinceParam = Uri.EscapeDataString(since.ToString("O"));
        var syncResponse = await employeeClient.GetAsync($"/api/v1/mobile/attendance/sync?since={sinceParam}&pageSize=100");
        var syncBody = await syncResponse.Content.ReadAsStringAsync();
        syncResponse.StatusCode.Should().Be(HttpStatusCode.OK, syncBody);

        // Asserting on presence/shape rather than the exact Date — the AttendanceDay's date is
        // resolved via the employee's *location* timezone (see ITimeZoneConverter), which can
        // legitimately differ from the test host's own UTC calendar date near a day boundary.
        var sync = JsonSerializer.Deserialize<DeltaSyncResponse>(syncBody, JsonOptions);
        sync!.Upserts.Should().ContainSingle();
        sync.Upserts[0].Status.Should().Be("Present");
        sync.TombstonedIds.Should().BeEmpty();
    }

    private sealed record PagedResult(IReadOnlyList<EmployeeSummary> Items);

    private sealed record EmployeeSummary(Guid Id, string Code);

    private sealed record DeltaSyncResponse(
        IReadOnlyList<AttendanceDaySummaryResponse> Upserts, IReadOnlyList<Guid> TombstonedIds, DateTimeOffset SyncedAt, string? NextCursor);

    private sealed record AttendanceDaySummaryResponse(Guid Id, DateOnly Date, string Status, int WorkedMinutes, bool RequiresApproval);
}
