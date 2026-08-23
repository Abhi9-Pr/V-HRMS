using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the Designations CRUD slice (M1) works end to end against the real pipeline —
/// copied from DepartmentsCrudTests.cs's shape.</summary>
public class DesignationsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public DesignationsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Should_Round_Trip_For_A_User_With_Designations_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-designations-crud");

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = "Quality Analyst", grade = 2 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var id = created!.Id;

        var getResponse = await client.GetAsync($"/api/v1/designations/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<DesignationResponse>();
        fetched!.Title.Should().Be("Quality Analyst");
        fetched.Grade.Should().Be(2);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/designations/{id}", new { title = "Senior Quality Analyst", grade = 3 });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterUpdateResponse = await client.GetAsync($"/api/v1/designations/{id}");
        var afterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<DesignationResponse>();
        afterUpdate!.Title.Should().Be("Senior Quality Analyst");
        afterUpdate.Grade.Should().Be(3);

        var listResponse = await client.GetAsync("/api/v1/designations?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/v1/designations/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await client.GetAsync($"/api/v1/designations/{id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_Designations_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-designations-forbidden");

        var response = await client.GetAsync("/api/v1/designations?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_For_A_User_Without_Designations_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-designations-create-forbidden");

        var response = await client.PostAsJsonAsync("/api/v1/designations", new { title = "Shadow Role", grade = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record DesignationResponse(Guid Id, string Title, int Grade);
}
