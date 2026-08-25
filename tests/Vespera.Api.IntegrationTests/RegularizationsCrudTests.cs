using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the regularization workflow (M6) works end to end against the real pipeline,
/// including that approval actually triggers a recompute through the real outbox — not just a
/// unit-tested handler in isolation. Background hosted services aren't registered under
/// "IntegrationTesting" (see VesperaWebApplicationFactory), so the outbox is drained manually here,
/// the same sanctioned way OffboardingCrudTests/OutboxDispatcherHostedServiceTests drive it directly.</summary>
public class RegularizationsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public RegularizationsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Other test classes sharing this factory's seed DB (AttendanceCrudTests,
    /// MobileAttendanceCrudTests) also punch priya and can leave an unclosed "In" punch behind
    /// (e.g. a geofence-rejected "Out" attempt never actually records). Deletes every existing
    /// AttendanceDay row for priya, regardless of date — the punch handler resolves the *employee's
    /// local* date via ITimeZoneConverter (Kolkata, UTC+5:30 for Head Office), which need not match
    /// a UTC-computed "today" client-side, so filtering the delete by a client-guessed date risks
    /// missing the actual row. Direct DB access, the same technique SeedGeofenceZoneAsync-style
    /// helpers already use elsewhere in this test project, gives this test a deterministic starting
    /// point instead of fighting over shared HTTP state.</summary>
    private async Task ResetPriyaAttendanceDayAsync(Guid priyaEmployeeId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var existing = await dbContext.Set<AttendanceDay>().IgnoreQueryFilters()
            .Where(d => d.TenantId == tenantId && d.EmployeeId == new EmployeeId(priyaEmployeeId))
            .ToListAsync();
        if (existing.Count > 0)
        {
            dbContext.RemoveRange(existing);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>Resolves the actual date the just-created AttendanceDay landed on (the employee's
    /// local date, per ITimeZoneConverter — not necessarily UTC-today), by asking the API for the
    /// employee's current day directly rather than re-deriving the timezone math client-side.</summary>
    private static async Task<AttendanceDayResponse> GetCurrentAttendanceDayAsync(HttpClient client, Guid employeeId)
    {
        foreach (var candidate in new[] { DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1) })
        {
            var response = await client.GetAsync($"/api/v1/attendance/days?employeeId={employeeId}&date={candidate:yyyy-MM-dd}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return (await response.Content.ReadFromJsonAsync<AttendanceDayResponse>(JsonOptions))!;
            }
        }

        throw new InvalidOperationException("Could not locate the attendance day just created by the In punch.");
    }

    private static async Task<Guid> GetPriyaEmployeeIdAsync(HttpClient hrClient)
    {
        var listResponse = await hrClient.GetAsync("/api/v1/employees?page=1&pageSize=50");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult>(JsonOptions);
        return page!.Items.Single(e => e.Code == "EMP-001").Id;
    }

    [Fact]
    public async Task Approving_A_Regularization_Should_Recompute_The_Attendance_Day_Through_The_Real_Outbox_Pipeline()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-regularization-crud");
        var priyaEmployeeId = await GetPriyaEmployeeIdAsync(hrClient);

        var (priyaClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-regularization-crud");

        await ResetPriyaAttendanceDayAsync(priyaEmployeeId);

        var inResponse = await priyaClient.PostAsJsonAsync(
            "/api/v1/attendance/punch", new { employeeId = priyaEmployeeId, punchType = "In", latitude = (double?)null, longitude = (double?)null });
        inResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await inResponse.Content.ReadAsStringAsync());

        var dayBefore = await GetCurrentAttendanceDayAsync(priyaClient, priyaEmployeeId);
        dayBefore.LastComputedAt.Should().BeNull("nothing has recomputed this day yet");

        using var submitContent = new MultipartFormDataContent
        {
            { new StringContent(dayBefore.Id.ToString()), "AttendanceDayId" },
            { new StringContent("Forgot to punch out before leaving early"), "Reason" },
        };
        var submitResponse = await priyaClient.PostAsync("/api/v1/regularizations", submitContent);
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, submitBody);
        var submitted = JsonSerializer.Deserialize<SubmitRegularizationResponse>(submitBody, JsonOptions)!;

        var approveResponse = await hrClient.PostAsync($"/api/v1/regularizations/{submitted.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await approveResponse.Content.ReadAsStringAsync());

        await DrainOutboxAsync();

        var dayAfter = await GetCurrentAttendanceDayAsync(priyaClient, priyaEmployeeId);

        dayAfter.LastComputedAt.Should().NotBeNull("approval should have triggered a recompute via the outbox-dispatched RegularizationApproved event");
        dayAfter.LastComputedBy.Should().Be("system");
    }

    [Fact]
    public async Task Approve_Should_Be_Forbidden_For_A_Caller_Who_Is_Not_The_Authorized_Approver()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-regularization-forbidden");
        var priyaEmployeeId = await GetPriyaEmployeeIdAsync(hrClient);

        var (priyaClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-regularization-forbidden");

        await ResetPriyaAttendanceDayAsync(priyaEmployeeId);
        var inResponse = await priyaClient.PostAsJsonAsync(
            "/api/v1/attendance/punch", new { employeeId = priyaEmployeeId, punchType = "In", latitude = (double?)null, longitude = (double?)null });
        inResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await inResponse.Content.ReadAsStringAsync());

        var day = await GetCurrentAttendanceDayAsync(priyaClient, priyaEmployeeId);

        using var submitContent = new MultipartFormDataContent
        {
            { new StringContent(day.Id.ToString()), "AttendanceDayId" },
            { new StringContent("Forgot to punch out"), "Reason" },
        };
        var submitResponse = await priyaClient.PostAsync("/api/v1/regularizations", submitContent);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitRegularizationResponse>(JsonOptions);

        // Priya herself is not an authorized approver of her own request (she's not her own manager).
        var approveResponse = await priyaClient.PostAsync($"/api/v1/regularizations/{submitted!.Id}/approve", null);

        approveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>Runs one real outbox-dispatch pass — the exact mechanism a live
    /// OutboxDispatcherHostedService would run on its poll interval, just invoked directly since
    /// that hosted service isn't registered under "IntegrationTesting".</summary>
    private async Task DrainOutboxAsync()
    {
        var dispatcher = new OutboxDispatcherHostedService(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BackgroundJobsOptions()),
            NullLogger<OutboxDispatcherHostedService>.Instance);

        await dispatcher.ProcessOnceAsync(CancellationToken.None);
    }

    private sealed record SubmitRegularizationResponse(Guid Id);

    private sealed record AttendanceDayResponse(Guid Id, DateTimeOffset? LastComputedAt, string? LastComputedBy);

    private sealed record PagedResult(IReadOnlyList<EmployeeSummary> Items);

    private sealed record EmployeeSummary(Guid Id, string Code);
}
