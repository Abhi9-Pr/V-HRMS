using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the Rosters slice (Attendance M2) works end to end against the real pipeline:
/// generate a draft roster from a rotation pattern, publish it, read the resolved grid, then prove
/// a per-employee override takes precedence over the base assignment for its date.</summary>
public class RostersCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public RostersCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Generate_Publish_Get_Should_Round_Trip_And_Override_Should_Take_Precedence()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-rosters-crud");

        var department = await CreateAsync(client, "/api/v1/departments", new { name = "Ops", code = $"OPS-{Guid.NewGuid():N}"[..16] });
        var designation = await CreateAsync(client, "/api/v1/designations", new { title = $"Analyst-{Guid.NewGuid():N}", grade = 2 });
        var location = await CreateAsync(client, "/api/v1/locations", new
        {
            name = "Ops Campus", addressLine = "1 Ops Way", city = "Pune", country = "India",
            latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata",
        });
        var employee = await CreateAsync(client, "/api/v1/employees", new
        {
            code = $"EMP-{Guid.NewGuid():N}"[..12],
            firstName = "Rosa",
            lastName = "Parks",
            workEmail = $"rosa.parks.{Guid.NewGuid():N}@vespera.test",
            phone = "+14155552673",
            dateOfBirth = "1990-01-01",
            dateOfJoining = "2026-01-15",
            departmentId = department.Id,
            designationId = designation.Id,
            locationId = location.Id,
        });
        var dayShift = await CreateAsync(client, "/api/v1/shifts", new { name = "Day Shift", startTime = "09:00:00", endTime = "18:00:00", graceMinutes = 10, breakMinutes = 0 });
        var nightShift = await CreateAsync(client, "/api/v1/shifts", new { name = "Night Shift", startTime = "22:00:00", endTime = "06:00:00", graceMinutes = 10, breakMinutes = 0 });
        var rotationPattern = await CreateAsync(client, "/api/v1/rotation-patterns", new
        {
            name = "Two Day Rotation",
            days = new object[]
            {
                new { sequenceNumber = 0, shiftId = dayShift.Id },
                new { sequenceNumber = 1, shiftId = dayShift.Id },
            },
        });

        var rangeStart = new DateOnly(2026, 6, 1);
        var rangeEnd = new DateOnly(2026, 6, 4);

        var generateResponse = await client.PostAsJsonAsync("/api/v1/rosters/generate", new
        {
            rotationPatternId = rotationPattern.Id,
            employeeIds = new[] { employee.Id },
            rangeStart,
            rangeEnd,
            patternAnchorDate = rangeStart,
        });
        var generateBody = await generateResponse.Content.ReadAsStringAsync();
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK, generateBody);
        var generated = JsonSerializer.Deserialize<GenerateRosterResponse>(generateBody, JsonOptions);
        generated!.RosterEntriesCreated.Should().BeGreaterThan(0);

        // Before publishing, the roster grid should show nothing (still Draft).
        var beforePublishResponse = await client.GetAsync(
            $"/api/v1/rosters?rangeStart={rangeStart:yyyy-MM-dd}&rangeEnd={rangeEnd:yyyy-MM-dd}&employeeId={employee.Id}");
        var beforePublishBody = await beforePublishResponse.Content.ReadAsStringAsync();
        beforePublishResponse.StatusCode.Should().Be(HttpStatusCode.OK, beforePublishBody);
        var beforePublish = JsonSerializer.Deserialize<RosterResultResponse>(beforePublishBody, JsonOptions);
        beforePublish!.Employees.Single().Days.Should().OnlyContain(day => day.ShiftId == null);

        var publishResponse = await client.PostAsJsonAsync("/api/v1/rosters/publish", new
        {
            employeeIds = new[] { employee.Id },
            rangeStart,
            rangeEnd,
        });
        var publishBody = await publishResponse.Content.ReadAsStringAsync();
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK, publishBody);

        var afterPublishResponse = await client.GetAsync(
            $"/api/v1/rosters?rangeStart={rangeStart:yyyy-MM-dd}&rangeEnd={rangeEnd:yyyy-MM-dd}&employeeId={employee.Id}");
        var afterPublish = await afterPublishResponse.Content.ReadFromJsonAsync<RosterResultResponse>();
        afterPublish!.Employees.Single().Days.Should().OnlyContain(day => day.ShiftId == dayShift.Id);

        var overrideDate = rangeStart.AddDays(1);
        var overrideResponse = await client.PostAsJsonAsync("/api/v1/rosters/overrides", new
        {
            employeeId = employee.Id,
            date = overrideDate,
            shiftId = nightShift.Id,
        });
        overrideResponse.EnsureSuccessStatusCode();

        var afterOverrideResponse = await client.GetAsync(
            $"/api/v1/rosters?rangeStart={rangeStart:yyyy-MM-dd}&rangeEnd={rangeEnd:yyyy-MM-dd}&employeeId={employee.Id}");
        var afterOverride = await afterOverrideResponse.Content.ReadFromJsonAsync<RosterResultResponse>();
        var days = afterOverride!.Employees.Single().Days;
        days.Single(day => day.Date == overrideDate).ShiftId.Should().Be(nightShift.Id);
        days.Where(day => day.Date != overrideDate).Should().OnlyContain(day => day.ShiftId == dayShift.Id);
    }

    [Fact]
    public async Task Get_Should_Return_Forbidden_For_A_User_Without_Rosters_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-rosters-forbidden");

        var response = await client.GetAsync("/api/v1/rosters?rangeStart=2026-06-01&rangeEnd=2026-06-02");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<CreatedResponse> CreateAsync(HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<CreatedResponse>(body, JsonOptions)!;
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record GenerateRosterResponse(int RosterEntriesCreated);

    private sealed record RosterDayResponse(DateOnly Date, Guid? ShiftId, string? ShiftName);

    private sealed record EmployeeRosterResponse(Guid EmployeeId, string EmployeeName, List<RosterDayResponse> Days);

    private sealed record RosterResultResponse(List<EmployeeRosterResponse> Employees);
}
