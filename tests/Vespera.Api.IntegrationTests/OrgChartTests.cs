using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the org chart slice (M5) works end to end: as-of-date tree shape, and ETag
/// caching that actually changes when the underlying data changes -- fixture helpers copied from
/// ReportingRelationshipsCrudTests.cs's shape.</summary>
public class OrgChartTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public OrgChartTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Should_Return_The_Tree_Then_304_On_Repeat_ETag_Then_200_After_A_Change()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-org-chart");

        var (departmentId, designationId, locationId) = await CreateMastersAsync(client);
        var manager = await CreateEmployeeAsync(client, departmentId, designationId, locationId);
        var report = await CreateEmployeeAsync(client, departmentId, designationId, locationId);

        var lineResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{report}/reports-to",
            new { managerId = manager, validFrom = "2026-01-01", validTo = (string?)null });
        lineResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstResponse = await client.GetAsync("/api/v1/org-chart?asOf=2026-06-01");
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, firstBody);
        var etag = firstResponse.Headers.ETag;
        etag.Should().NotBeNull();

        var nodes = JsonSerializer.Deserialize<List<OrgChartNodeResponse>>(firstBody, JsonOptions);
        var managerNode = nodes.Should().ContainSingle(n => n.EmployeeId == manager).Subject;
        managerNode.Children.Should().ContainSingle(c => c.EmployeeId == report);

        using var repeatRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/org-chart?asOf=2026-06-01");
        repeatRequest.Headers.IfNoneMatch.Add(etag!);
        var repeatResponse = await client.SendAsync(repeatRequest);
        repeatResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);

        // A new reporting relationship changes the underlying data -- the same If-None-Match
        // must no longer short-circuit.
        var thirdEmployee = await CreateEmployeeAsync(client, departmentId, designationId, locationId);
        var secondLineResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{thirdEmployee}/reports-to",
            new { managerId = manager, validFrom = "2026-01-01", validTo = (string?)null });
        secondLineResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var staleRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/org-chart?asOf=2026-06-01");
        staleRequest.Headers.IfNoneMatch.Add(etag!);
        var freshResponse = await client.SendAsync(staleRequest);
        freshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        freshResponse.Headers.ETag.Should().NotBe(etag);
    }

    [Fact]
    public async Task Get_Should_Return_Forbidden_For_A_User_Without_OrgChart_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-org-chart-forbidden");

        var response = await client.GetAsync("/api/v1/org-chart?asOf=2026-06-01");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<(Guid DepartmentId, Guid DesignationId, Guid LocationId)> CreateMastersAsync(HttpClient client)
    {
        var departmentResponse = await client.PostAsJsonAsync(
            "/api/v1/departments", new { name = "Org Chart", code = $"ORG-{Guid.NewGuid():N}"[..16] });
        var department = await departmentResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Engineer-{Guid.NewGuid():N}", grade = 3 });
        var designation = await designationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "Org Chart Campus", addressLine = "1 Org Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
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

    private sealed record OrgChartNodeResponse(Guid EmployeeId, string Code, string FullName, string Status, string? DesignationTitle, string? DepartmentName, List<OrgChartNodeResponse> Children);
}
