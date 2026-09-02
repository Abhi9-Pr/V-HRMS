using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the reference CRUD slice works end to end against the real pipeline — this is
/// what "Departments CRUD works" (Phase 5's done-when criterion) actually verifies.</summary>
public class DepartmentsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public DepartmentsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Should_Round_Trip_For_A_User_With_Departments_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-departments-crud");

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/departments", new { name = "Quality Assurance", code = $"QA-{Guid.NewGuid():N}"[..10] });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var id = created!.Id;

        var getResponse = await client.GetAsync($"/api/v1/departments/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<DepartmentResponse>();
        fetched!.Name.Should().Be("Quality Assurance");

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/departments/{id}", new { name = "Quality Engineering", parentDepartmentId = (Guid?)null });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterUpdateResponse = await client.GetAsync($"/api/v1/departments/{id}");
        var afterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<DepartmentResponse>();
        afterUpdate!.Name.Should().Be("Quality Engineering");

        var listResponse = await client.GetAsync("/api/v1/departments?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/v1/departments/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await client.GetAsync($"/api/v1/departments/{id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_Departments_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-departments-forbidden");

        var response = await client.GetAsync("/api/v1/departments?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_For_A_User_Without_Departments_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-departments-create-forbidden");

        var response = await client.PostAsJsonAsync("/api/v1/departments", new { name = "Shadow IT", code = "SHADOW" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Update_Delete_Should_Return_NotFound_For_A_Department_Owned_By_Another_Tenant()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-departments-idor-owner");

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/departments", new { name = "Cross-Tenant Target", code = $"XT-{Guid.NewGuid():N}"[..10] });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await createResponse.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        (await otherTenantClient.GetAsync($"/api/v1/departments/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.PutAsJsonAsync(
            $"/api/v1/departments/{id}", new { name = "Hijacked", parentDepartmentId = (Guid?)null })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.DeleteAsync($"/api/v1/departments/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record DepartmentResponse(Guid Id, string Name, string Code, Guid? ParentDepartmentId);
}
