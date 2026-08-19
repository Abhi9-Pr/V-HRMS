using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Vespera.Domain.Payroll;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves IdempotencyMiddleware replays the original response rather than re-executing
/// the handler: finalizing an already-finalized payroll run fails (Conflict), so if the second,
/// identically-keyed request came back as anything other than the exact original 204, the handler
/// would have run twice.</summary>
public class IdempotencyReplayTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public IdempotencyReplayTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Replaying_The_Same_Idempotency_Key_Should_Return_The_Original_Response_Not_Re_Execute()
    {
        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "fatima.khan@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-fatima-idempotency", code);
        var payrollRunId = await GetSeededPayrollRunIdAsync();
        var idempotencyKey = Guid.NewGuid().ToString();

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/finance/payroll-runs/{payrollRunId}/finalize");
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await client.SendAsync(firstRequest);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/finance/payroll-runs/{payrollRunId}/finalize");
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var second = await client.SendAsync(secondRequest);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // If the handler had actually run a second time, finalizing an already-finalized run
        // fails with Conflict (409) — a replayed response must be the identical 204 instead.
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Guid> GetSeededPayrollRunIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var run = await dbContext.Set<PayrollRun>().IgnoreQueryFilters().FirstAsync();
        return run.Id.Value;
    }
}
