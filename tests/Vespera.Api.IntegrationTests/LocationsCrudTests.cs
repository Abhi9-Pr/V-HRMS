using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the Locations CRUD slice (M1) works end to end against the real pipeline —
/// copied from DepartmentsCrudTests.cs's shape.</summary>
public class LocationsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public LocationsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Should_Round_Trip_For_A_User_With_Locations_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-locations-crud");

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new
            {
                name = "Pune Office",
                addressLine = "Hinjewadi Phase 2",
                city = "Pune",
                country = "India",
                latitude = 18.5913,
                longitude = 73.7389,
                timeZoneId = "Asia/Kolkata",
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var id = created!.Id;

        var getResponse = await client.GetAsync($"/api/v1/locations/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<LocationResponse>();
        fetched!.Name.Should().Be("Pune Office");
        fetched.City.Should().Be("Pune");

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/locations/{id}",
            new { latitude = 19.0760, longitude = 72.8777, addressLine = "Bandra Kurla Complex", city = "Mumbai", country = "India" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterUpdateResponse = await client.GetAsync($"/api/v1/locations/{id}");
        var afterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<LocationResponse>();
        afterUpdate!.City.Should().Be("Mumbai");

        var listResponse = await client.GetAsync("/api/v1/locations?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/v1/locations/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await client.GetAsync($"/api/v1/locations/{id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_Locations_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-locations-forbidden");

        var response = await client.GetAsync("/api/v1/locations?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_For_A_User_Without_Locations_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-locations-create-forbidden");

        var response = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "Shadow Office", addressLine = "N/A", city = "N/A", country = "N/A", latitude = 0.0, longitude = 0.0, timeZoneId = "UTC" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Update_Delete_Should_Return_NotFound_For_A_Location_Owned_By_Another_Tenant()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-locations-idor-owner");

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "Cross-Tenant Target", addressLine = "1 Isolation Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await createResponse.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        (await otherTenantClient.GetAsync($"/api/v1/locations/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.PutAsJsonAsync(
            $"/api/v1/locations/{id}",
            new { latitude = 0.0, longitude = 0.0, addressLine = "Hijacked", city = "Hijacked", country = "Hijacked" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.DeleteAsync($"/api/v1/locations/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record LocationResponse(
        Guid Id, string Name, string AddressLine, string City, string Country, double Latitude, double Longitude, string TimeZoneId);
}
