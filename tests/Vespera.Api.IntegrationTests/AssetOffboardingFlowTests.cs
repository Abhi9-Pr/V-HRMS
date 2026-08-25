using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>End-to-end proof of Phase 11b's employee-exit trigger: a license seat allocated to an
/// employee and an asset assigned to them are both released/recovered automatically once the
/// employee exits — driven entirely through the real API, with the domain-event side effect
/// replayed by manually pumping the outbox dispatcher once (it isn't registered as a hosted
/// service under the IntegrationTesting environment — see Program.cs — so the test drives
/// OutboxDispatcherHostedService.ProcessOnceAsync itself, exactly as its own doc comment
/// anticipates).</summary>
public class AssetOffboardingFlowTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public AssetOffboardingFlowTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Employee_Exit_Releases_License_Seats_And_Initiates_Asset_Recovery()
    {
        // Rohan (HR + Manager) administers assets/licenses and records Ananya's exit.
        var (rohanClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-offboarding");

        var ananyaId = await GetEmployeeIdByEmailAsync("ananya.iyer@demo.vespera.test");

        var createAssetResponse = await rohanClient.PostAsJsonAsync("/api/v1/assets", new
        {
            assetTag = $"AST-{Guid.NewGuid():N}"[..12],
            category = "Laptop",
            purchaseCost = 80000m,
            purchaseCostCurrency = Currency.Inr,
            purchaseDate = new DateOnly(2026, 1, 1),
            serialNumber = (string?)null,
            macAddress = (string?)null,
            warrantyExpiryDate = (DateOnly?)null,
        });
        createAssetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var assetId = (await createAssetResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var assignResponse = await rohanClient.PostAsJsonAsync($"/api/v1/assets/{assetId}/assign", new { employeeId = ananyaId });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createLicenseResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/licenses", new { productName = $"Figma-{Guid.NewGuid():N}", seatCount = 5, expiresAt = (DateOnly?)null });
        createLicenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var licenseId = (await createLicenseResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var allocateResponse = await rohanClient.PostAsJsonAsync($"/api/v1/licenses/{licenseId}/allocations", new { employeeId = ananyaId });
        allocateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var exitResponse = await rohanClient.PostAsJsonAsync(
            $"/api/v1/employees/{ananyaId}/exit", new { exitDate = new DateOnly(2026, 6, 1), reason = "Resignation" });
        exitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await DrainOutboxAsync();

        var unusedSeatsResponse = await rohanClient.GetAsync("/api/v1/licenses/unused-seats-report");
        unusedSeatsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await unusedSeatsResponse.Content.ReadFromJsonAsync<List<UnusedSeatsRow>>();
        report.Should().ContainSingle(row => row.LicenseId == licenseId && row.UnusedSeats == 5);

        var pendingRecoveriesResponse = await rohanClient.GetAsync("/api/v1/asset-recoveries?pageSize=50");
        pendingRecoveriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var recoveries = await pendingRecoveriesResponse.Content.ReadFromJsonAsync<PagedResponse<RecoveryRow>>();
        recoveries!.Items.Should().ContainSingle(r => r.AssetId == assetId && r.EmployeeId == ananyaId);

        var checklistResponse = await rohanClient.GetAsync($"/api/v1/offboarding-checklists/employees/{ananyaId}");
        checklistResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checklist = await checklistResponse.Content.ReadFromJsonAsync<ChecklistResponse>();
        checklist!.Items.Should().ContainSingle(i => i.Description.Contains(assetId.ToString()));
    }

    private async Task DrainOutboxAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dispatcher = new OutboxDispatcherHostedService(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BackgroundJobsOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<OutboxDispatcherHostedService>>());

        await dispatcher.ProcessOnceAsync(CancellationToken.None);
    }

    private async Task<Guid> GetEmployeeIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();

        var emailAddress = EmailAddress.Create(email).Value;
        var employee = await dbContext.Set<Employee>().IgnoreQueryFilters().FirstAsync(e => e.WorkEmail == emailAddress);
        return employee.Id.Value;
    }

    private sealed record IdResponse(Guid Id);

    private sealed record UnusedSeatsRow(Guid LicenseId, string ProductName, int SeatCount, int SeatsUsed, int UnusedSeats, DateOnly? ExpiresAt);

    private sealed record RecoveryRow(Guid Id, Guid AssetAssignmentId, Guid AssetId, Guid EmployeeId);

    private sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    private sealed record ChecklistItemResponse(string Description, bool IsComplete);

    private sealed record ChecklistResponse(Guid Id, Guid EmployeeId, bool IsComplete, IReadOnlyList<ChecklistItemResponse> Items);
}
