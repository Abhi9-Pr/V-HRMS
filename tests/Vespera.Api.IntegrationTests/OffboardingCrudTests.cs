using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the exit -> checklist-creation path fires through the real pipeline (a domain
/// event dispatched via the outbox, not just a unit-tested handler in isolation) and that the two
/// placeholder confirmations work end to end. Background hosted services aren't registered under
/// "IntegrationTesting" (see VesperaWebApplicationFactory) so the outbox is drained manually here,
/// the same sanctioned way OutboxDispatcherHostedServiceTests drives it directly.</summary>
public class OffboardingCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public OffboardingCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Exit_Should_Initiate_A_Checklist_Through_The_Real_Outbox_Pipeline_And_Confirmations_Should_Round_Trip()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-offboarding-crud");

        var departmentResponse = await client.PostAsJsonAsync("/api/v1/departments", new { name = "Ops", code = $"OPS-{Guid.NewGuid():N}"[..16] });
        var department = await departmentResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Analyst-{Guid.NewGuid():N}", grade = 2 });
        var designation = await designationResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "Ops Campus", addressLine = "1 Exit Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var createResponse = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            code = $"EMP-{Guid.NewGuid():N}"[..12],
            firstName = "Leaving",
            lastName = "Soon",
            workEmail = $"leaving.soon.{Guid.NewGuid():N}@vespera.test",
            phone = "+14155552671",
            dateOfBirth = "1990-01-01",
            dateOfJoining = "2020-01-15",
            departmentId = department!.Id,
            designationId = designation!.Id,
            locationId = location!.Id,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var employeeId = created!.Id;

        var exitResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employeeId}/exit", new { exitDate = "2026-01-31", reason = "Resignation" });
        exitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await DrainOutboxAsync();

        var checklistResponse = await client.GetAsync($"/api/v1/employees/{employeeId}/offboarding");
        var checklistBody = await checklistResponse.Content.ReadAsStringAsync();
        checklistResponse.StatusCode.Should().Be(HttpStatusCode.OK, checklistBody);
        var checklist = JsonSerializer.Deserialize<OffboardingChecklistResponse>(checklistBody, JsonOptions);
        checklist!.EmployeeId.Should().Be(employeeId);
        checklist.AccessRevokedStatus.Should().Be("Pending");
        checklist.AssetsRecoveredStatus.Should().Be("Pending");
        checklist.FinalSettlementStatus.Should().Be("Pending");

        var confirmAssetsResponse = await client.PostAsync($"/api/v1/employees/{employeeId}/offboarding/confirm-assets-recovered", null);
        confirmAssetsResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var confirmSettlementResponse = await client.PostAsync($"/api/v1/employees/{employeeId}/offboarding/confirm-final-settlement", null);
        confirmSettlementResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var finalChecklistResponse = await client.GetAsync($"/api/v1/employees/{employeeId}/offboarding");
        var finalChecklist = await finalChecklistResponse.Content.ReadFromJsonAsync<OffboardingChecklistResponse>();
        finalChecklist!.AssetsRecoveredStatus.Should().Be("Done");
        finalChecklist.FinalSettlementStatus.Should().Be("Done");
    }

    [Fact]
    public async Task Get_Should_Return_Forbidden_For_A_User_Without_Offboarding_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-offboarding-forbidden");

        var response = await client.GetAsync("/api/v1/employees/00000000-0000-0000-0000-000000000000/offboarding");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Should_Return_NotFound_For_A_Checklist_Owned_By_Another_Tenant()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-offboarding-idor-owner");

        var departmentResponse = await client.PostAsJsonAsync("/api/v1/departments", new { name = "Ops IDOR", code = $"OPI-{Guid.NewGuid():N}"[..16] });
        var department = await departmentResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Analyst-{Guid.NewGuid():N}", grade = 2 });
        var designation = await designationResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "Ops IDOR Campus", addressLine = "1 Exit Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var createResponse = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            code = $"EMP-{Guid.NewGuid():N}"[..12],
            firstName = "Leaving",
            lastName = "IdorTarget",
            workEmail = $"leaving.idor.{Guid.NewGuid():N}@vespera.test",
            phone = "+14155552671",
            dateOfBirth = "1990-01-01",
            dateOfJoining = "2020-01-15",
            departmentId = department!.Id,
            designationId = designation!.Id,
            locationId = location!.Id,
        });
        var employeeId = (await createResponse.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var exitResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employeeId}/exit", new { exitDate = "2026-01-31", reason = "Resignation" });
        exitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await DrainOutboxAsync();

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        (await otherTenantClient.GetAsync($"/api/v1/employees/{employeeId}/offboarding")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.PostAsync($"/api/v1/employees/{employeeId}/offboarding/confirm-assets-recovered", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Runs one real outbox-dispatch pass — the exact mechanism a live
    /// OutboxDispatcherHostedService would run on its poll interval, just invoked directly since
    /// that hosted service isn't registered under "IntegrationTesting".</summary>
    private async Task DrainOutboxAsync()
    {
        var dispatcher = new OutboxDispatcherHostedService(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BackgroundJobsOptions()),
            NullLogger<OutboxDispatcherHostedService>.Instance);

        await dispatcher.ProcessOnceAsync(CancellationToken.None);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record OffboardingChecklistResponse(
        Guid Id, Guid EmployeeId, string AccessRevokedStatus, string AssetsRecoveredStatus, string FinalSettlementStatus);
}
