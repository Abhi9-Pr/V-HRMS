using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the Shifts CRUD slice (Attendance M1) works end to end against the real
/// pipeline — copied from DepartmentsCrudTests.cs's shape.</summary>
public class ShiftsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public ShiftsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Should_Round_Trip_For_A_User_With_Shifts_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-shifts-crud");

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/shifts", new { name = "Day Shift", startTime = "09:00:00", endTime = "18:00:00", graceMinutes = 10, breakMinutes = 30 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var id = created!.Id;

        var getResponse = await client.GetAsync($"/api/v1/shifts/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ShiftResponse>();
        fetched!.Name.Should().Be("Day Shift");
        fetched.BreakMinutes.Should().Be(30);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/shifts/{id}", new { name = "Evening Shift", startTime = "14:00:00", endTime = "22:00:00", breakMinutes = 45 });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterUpdateResponse = await client.GetAsync($"/api/v1/shifts/{id}");
        var afterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<ShiftResponse>();
        afterUpdate!.Name.Should().Be("Evening Shift");
        afterUpdate.BreakMinutes.Should().Be(45);

        var listResponse = await client.GetAsync("/api/v1/shifts?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/v1/shifts/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await client.GetAsync($"/api/v1/shifts/{id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_Shifts_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-shifts-forbidden");

        var response = await client.GetAsync("/api/v1/shifts?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_For_A_User_Without_Shifts_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-shifts-create-forbidden");

        var response = await client.PostAsJsonAsync(
            "/api/v1/shifts", new { name = "Shadow Shift", startTime = "09:00:00", endTime = "18:00:00", graceMinutes = 0, breakMinutes = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_Should_Return_304_On_Repeat_ETag_Then_200_After_A_Change()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-shifts-etag");

        var firstResponse = await client.GetAsync("/api/v1/shifts?page=1&pageSize=50");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = firstResponse.Headers.ETag;
        etag.Should().NotBeNull();

        using var repeatRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/shifts?page=1&pageSize=50");
        repeatRequest.Headers.IfNoneMatch.Add(etag!);
        var repeatResponse = await client.SendAsync(repeatRequest);
        repeatResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/shifts", new { name = $"ETag Shift {Guid.NewGuid():N}", startTime = "08:00:00", endTime = "17:00:00", graceMinutes = 5, breakMinutes = 30 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var staleRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/shifts?page=1&pageSize=50");
        staleRequest.Headers.IfNoneMatch.Add(etag!);
        var freshResponse = await client.SendAsync(staleRequest);
        freshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        freshResponse.Headers.ETag.Should().NotBe(etag);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record ShiftResponse(Guid Id, string Name, int BreakMinutes);
}
