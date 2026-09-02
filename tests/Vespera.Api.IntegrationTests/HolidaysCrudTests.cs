using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the Holidays CRUD slice (Attendance M1) works end to end against the real
/// pipeline — copied from DepartmentsCrudTests.cs's shape.</summary>
public class HolidaysCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public HolidaysCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Should_Round_Trip_For_A_User_With_Holidays_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-holidays-crud");

        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "R&D Campus", addressLine = "2 Innovation Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        locationResponse.EnsureSuccessStatusCode();
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/holidays", new { locationId = location!.Id, date = "2026-01-26", name = "Republic Day" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var id = created!.Id;

        var getResponse = await client.GetAsync($"/api/v1/holidays/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<HolidayResponse>();
        fetched!.Name.Should().Be("Republic Day");
        fetched.LocationId.Should().Be(location.Id);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/holidays/{id}", new { name = "Republic Day (Observed)", date = "2026-01-27" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterUpdateResponse = await client.GetAsync($"/api/v1/holidays/{id}");
        var afterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<HolidayResponse>();
        afterUpdate!.Name.Should().Be("Republic Day (Observed)");

        var listResponse = await client.GetAsync($"/api/v1/holidays?page=1&pageSize=50&locationId={location.Id}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/v1/holidays/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await client.GetAsync($"/api/v1/holidays/{id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_Holidays_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-holidays-forbidden");

        var response = await client.GetAsync("/api/v1/holidays?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_For_A_User_Without_Holidays_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-holidays-create-forbidden");

        var response = await client.PostAsJsonAsync(
            "/api/v1/holidays", new { locationId = Guid.NewGuid(), date = "2026-01-26", name = "Shadow Holiday" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_Should_Return_304_On_Repeat_ETag_Then_200_After_A_Change()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-holidays-etag");

        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "ETag Campus", addressLine = "3 Cache Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        locationResponse.EnsureSuccessStatusCode();
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var listUrl = $"/api/v1/holidays?page=1&pageSize=50&locationId={location!.Id}";
        var firstResponse = await client.GetAsync(listUrl);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = firstResponse.Headers.ETag;
        etag.Should().NotBeNull();

        using var repeatRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
        repeatRequest.Headers.IfNoneMatch.Add(etag!);
        var repeatResponse = await client.SendAsync(repeatRequest);
        repeatResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/holidays", new { locationId = location.Id, date = "2026-08-15", name = "Independence Day" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var staleRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
        staleRequest.Headers.IfNoneMatch.Add(etag!);
        var freshResponse = await client.SendAsync(staleRequest);
        freshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        freshResponse.Headers.ETag.Should().NotBe(etag);
    }

    [Fact]
    public async Task Get_Update_Delete_Should_Return_NotFound_For_A_Holiday_Owned_By_Another_Tenant()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-holidays-idor-owner");

        var locationResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "IDOR Campus", addressLine = "1 Isolation Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        locationResponse.EnsureSuccessStatusCode();
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/holidays", new { locationId = location!.Id, date = "2026-03-01", name = "Cross-Tenant Target" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await createResponse.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        (await otherTenantClient.GetAsync($"/api/v1/holidays/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.PutAsJsonAsync(
            $"/api/v1/holidays/{id}", new { name = "Hijacked", date = "2026-03-02" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.DeleteAsync($"/api/v1/holidays/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record HolidayResponse(Guid Id, Guid LocationId, string Name);
}
