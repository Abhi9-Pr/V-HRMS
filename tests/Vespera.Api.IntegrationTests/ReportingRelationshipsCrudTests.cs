using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the reporting-relationships slice (M4) works end to end against the real
/// pipeline, including cycle detection over an actual HTTP round trip -- copied from
/// EmployeesCrudTests.cs's shape.</summary>
public class ReportingRelationshipsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public ReportingRelationshipsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_List_And_End_Should_Round_Trip_And_A_Cycle_Should_Be_Rejected()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-reporting-relationships-crud");

        var (departmentId, designationId, locationId) = await CreateMastersAsync(client);
        var employee1 = await CreateEmployeeAsync(client, departmentId, designationId, locationId);
        var employee2 = await CreateEmployeeAsync(client, departmentId, designationId, locationId);
        var employee3 = await CreateEmployeeAsync(client, departmentId, designationId, locationId);

        // employee2 reports to employee1.
        var firstLineResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employee2}/reports-to",
            new { managerId = employee1, validFrom = "2026-01-01", validTo = (string?)null });
        var firstLineBody = await firstLineResponse.Content.ReadAsStringAsync();
        firstLineResponse.StatusCode.Should().Be(HttpStatusCode.OK, firstLineBody);
        var firstLine = JsonSerializer.Deserialize<CreatedResponse>(firstLineBody, JsonOptions);

        // employee3 reports to employee2.
        var secondLineResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employee3}/reports-to",
            new { managerId = employee2, validFrom = "2026-01-01", validTo = (string?)null });
        secondLineResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // employee1 reporting to employee3 would close the cycle 1 -> 3 -> 2 -> 1.
        var cycleResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employee1}/reports-to",
            new { managerId = employee3, validFrom = "2026-06-01", validTo = (string?)null });
        var cycleBody = await cycleResponse.Content.ReadAsStringAsync();
        cycleResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, cycleBody);

        var listResponse = await client.GetAsync($"/api/v1/employees/{employee2}/reports-to");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lines = await listResponse.Content.ReadFromJsonAsync<List<ReportingRelationshipResponse>>();
        lines.Should().ContainSingle(line => line.Id == firstLine!.Id && line.ManagerId == employee1);

        var endResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employee2}/reports-to/{firstLine!.Id}/end", new { validTo = "2026-06-30" });
        endResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_For_A_User_Without_ReportingRelationships_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-reporting-relationships-forbidden");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/employees/{Guid.NewGuid()}/reports-to",
            new { managerId = Guid.NewGuid(), validFrom = "2026-01-01", validTo = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_Should_Return_Empty_And_End_Should_Return_NotFound_For_A_Line_Owned_By_Another_Tenant()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-reporting-relationships-idor-owner");

        var (departmentId, designationId, locationId) = await CreateMastersAsync(client);
        var manager = await CreateEmployeeAsync(client, departmentId, designationId, locationId);
        var report = await CreateEmployeeAsync(client, departmentId, designationId, locationId);

        var lineResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{report}/reports-to", new { managerId = manager, validFrom = "2026-01-01", validTo = (string?)null });
        var lineId = (await lineResponse.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        // Unlike EmployeeDocumentsCrudTests' list endpoint, this one does not validate that
        // employeeId belongs to the caller's tenant before listing — it returns whatever the
        // tenant-scoped query filter leaves, which for another tenant's employeeId is always
        // empty. No cross-tenant data is exposed either way; the response shape just differs.
        var listResponse = await otherTenantClient.GetAsync($"/api/v1/employees/{report}/reports-to");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lines = await listResponse.Content.ReadFromJsonAsync<List<ReportingRelationshipResponse>>();
        lines.Should().BeEmpty();
        (await otherTenantClient.PostAsJsonAsync($"/api/v1/employees/{report}/reports-to/{lineId}/end", new { validTo = "2026-06-30" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<(Guid DepartmentId, Guid DesignationId, Guid LocationId)> CreateMastersAsync(HttpClient client)
    {
        var departmentResponse = await client.PostAsJsonAsync(
            "/api/v1/departments", new { name = "Reporting Lines", code = $"RPT-{Guid.NewGuid():N}"[..16] });
        var department = await departmentResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Engineer-{Guid.NewGuid():N}", grade = 3 });
        var designation = await designationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "Reporting Lines Campus", addressLine = "1 Org Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        return (department!.Id, designation!.Id, location!.Id);
    }

    private static async Task<Guid> CreateEmployeeAsync(HttpClient client, Guid departmentId, Guid designationId, Guid locationId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            code = $"EMP-{Guid.NewGuid():N}"[..12],
            firstName = "Test",
            lastName = $"Employee-{Guid.NewGuid():N}"[..20],
            workEmail = $"employee.{Guid.NewGuid():N}@vespera.test",
            phone = "+14155552671",
            dateOfBirth = "1990-01-01",
            dateOfJoining = "2026-01-15",
            departmentId,
            designationId,
            locationId,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var created = JsonSerializer.Deserialize<CreatedResponse>(body, JsonOptions);
        return created!.Id;
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record ReportingRelationshipResponse(Guid Id, Guid EmployeeId, Guid ManagerId, DateOnly ValidFrom, DateOnly? ValidTo);
}
