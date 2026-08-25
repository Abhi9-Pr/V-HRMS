using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the RotationPatterns CRUD slice (Attendance M1) works end to end against the
/// real pipeline — copied from DepartmentsCrudTests.cs's shape.</summary>
public class RotationPatternsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public RotationPatternsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Should_Round_Trip_For_A_User_With_RotationPatterns_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-rotation-patterns-crud");

        var shiftResponse = await client.PostAsJsonAsync(
            "/api/v1/shifts", new { name = "Day Shift", startTime = "09:00:00", endTime = "18:00:00", graceMinutes = 10, breakMinutes = 0 });
        shiftResponse.EnsureSuccessStatusCode();
        var shift = await shiftResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/rotation-patterns",
            new
            {
                name = "3-Day Rotation",
                days = new object[]
                {
                    new { sequenceNumber = 0, shiftId = shift!.Id },
                    new { sequenceNumber = 1, shiftId = (Guid?)null },
                    new { sequenceNumber = 2, shiftId = (Guid?)null },
                },
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var id = created!.Id;

        var getResponse = await client.GetAsync($"/api/v1/rotation-patterns/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<RotationPatternResponse>();
        fetched!.Name.Should().Be("3-Day Rotation");
        fetched.Days.Should().HaveCount(3);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/rotation-patterns/{id}",
            new { days = new object[] { new { sequenceNumber = 0, shiftId = shift.Id }, new { sequenceNumber = 1, shiftId = (Guid?)null } } });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterUpdateResponse = await client.GetAsync($"/api/v1/rotation-patterns/{id}");
        var afterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<RotationPatternResponse>();
        afterUpdate!.Days.Should().HaveCount(2);

        var listResponse = await client.GetAsync("/api/v1/rotation-patterns?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await client.DeleteAsync($"/api/v1/rotation-patterns/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await client.GetAsync($"/api/v1/rotation-patterns/{id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_RotationPatterns_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-rotation-patterns-forbidden");

        var response = await client.GetAsync("/api/v1/rotation-patterns?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_For_A_User_Without_RotationPatterns_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-rotation-patterns-create-forbidden");

        var response = await client.PostAsJsonAsync(
            "/api/v1/rotation-patterns",
            new { name = "Shadow Pattern", days = new object[] { new { sequenceNumber = 0, shiftId = (Guid?)null } } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record RotationPatternDayResponse(int SequenceNumber, Guid? ShiftId);

    private sealed record RotationPatternResponse(Guid Id, string Name, List<RotationPatternDayResponse> Days);
}
