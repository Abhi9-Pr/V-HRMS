using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the attendance-grid read (added in the performance-hardening phase) works end
/// to end: an employee's already-computed AttendanceDay rows show up on the right dates, a date
/// with no computed row comes back with a null status rather than being silently dropped, and
/// cross-tenant access is blocked the same way every other resource-scoped endpoint is.</summary>
public class AttendanceGridCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public AttendanceGridCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid EmployeeId, DateOnly Date)> SeedAttendanceDayAsync(string employeeCode, AttendanceDayStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var code = EmployeeCode.Create(employeeCode).Value;
        var employee = await dbContext.Set<Employee>().IgnoreQueryFilters()
            .FirstAsync(e => e.TenantId == tenantId && e.Code == code);

        var date = new DateOnly(2026, 6, 1);
        var day = AttendanceDay.Open(tenantId, employee.Id, date);
        day.ApplyComputation(
            new AttendanceComputationResult(null, null, 0, 0, 0, 0, false, status), DateTimeOffset.UtcNow, "test");
        dbContext.Add(day);
        await dbContext.SaveChangesAsync();

        return (employee.Id.Value, date);
    }

    [Fact]
    public async Task Get_Should_Return_The_Seeded_Status_On_Its_Date_And_Null_On_Days_With_No_Computed_Row()
    {
        var (employeeId, date) = await SeedAttendanceDayAsync("EMP-001", AttendanceDayStatus.Present);

        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "admin@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-admin-attendance-grid");

        var response = await client.GetAsync(
            $"/api/v1/attendance/grid?rangeStart={date:yyyy-MM-dd}&rangeEnd={date.AddDays(1):yyyy-MM-dd}&pageSize=200");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var page = await response.Content.ReadFromJsonAsync<GridPage>();
        var row = page!.Items.Should().Contain(r => r.EmployeeId == employeeId).Subject;
        row.Days.Should().HaveCount(2);
        row.Days.Should().Contain(day => day.Date == date && day.Status == "Present");
        row.Days.Should().Contain(day => day.Date == date.AddDays(1) && day.Status == null);
    }

    [Fact]
    public async Task Get_Should_Return_Forbidden_For_A_User_Without_Attendance_ReadTeam()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-attendance-grid-forbidden");

        var response = await client.GetAsync("/api/v1/attendance/grid?rangeStart=2026-06-01&rangeEnd=2026-06-01");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Should_Return_BadRequest_When_The_Date_Range_Exceeds_31_Days()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "admin@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-admin-attendance-grid-range");

        var response = await client.GetAsync("/api/v1/attendance/grid?rangeStart=2026-01-01&rangeEnd=2026-03-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Should_Not_Return_An_Employee_Owned_By_Another_Tenant()
    {
        var (employeeId, date) = await SeedAttendanceDayAsync("EMP-002", AttendanceDayStatus.Present);

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        var response = await otherTenantClient.GetAsync(
            $"/api/v1/attendance/grid?rangeStart={date:yyyy-MM-dd}&rangeEnd={date:yyyy-MM-dd}&pageSize=200");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<GridPage>();
        page!.Items.Should().NotContain(row => row.EmployeeId == employeeId, "this employee belongs to a different tenant");
    }

    private sealed record GridPage(IReadOnlyList<GridRow> Items);

    private sealed record GridRow(Guid EmployeeId, string EmployeeCode, string EmployeeName, IReadOnlyList<GridDay> Days);

    private sealed record GridDay(DateOnly Date, string? Status);
}
