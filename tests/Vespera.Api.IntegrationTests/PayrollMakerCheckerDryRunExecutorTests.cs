using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>
/// Extends FinancePolicyWallTests' maker-checker coverage with the specific case the brief calls
/// out: "the user who ran the dry run cannot finalize" is a distinct check from "the creator cannot
/// finalize" — one person can open a run, a different person can trigger the compute, and *both*
/// must be blocked independently. The seeded demo payroll run has the same person as both creator
/// and dry-run executor, which can't distinguish the two checks, so this builds a second run here
/// with Fatima as creator and Vikram as dry-run executor specifically.
/// </summary>
public class PayrollMakerCheckerDryRunExecutorTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public PayrollMakerCheckerDryRunExecutorTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_DryRun_Executor_Should_Get_403_Finalizing_Even_Though_They_Did_Not_Create_The_Run()
    {
        var (creatorId, executorId, payrollRunId) = await CreateRunWithDistinctCreatorAndDryRunExecutorAsync();

        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "vikram.nair@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-vikram-dryrun-executor", code);

        var response = await client.PostAsync($"/api/v1/finance/payroll-runs/{payrollRunId}/finalize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Vikram ran the dry-run compute on this run, even though Fatima created it");
    }

    [Fact]
    public async Task The_Creator_Should_Still_Get_403_Even_When_They_Are_Not_The_DryRun_Executor()
    {
        var (creatorId, executorId, payrollRunId) = await CreateRunWithDistinctCreatorAndDryRunExecutorAsync();

        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "fatima.khan@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-fatima-creator-only", code);

        var response = await client.PostAsync($"/api/v1/finance/payroll-runs/{payrollRunId}/finalize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Fatima created this run, even though Vikram (not her) ran its dry-run compute");
    }

    [Fact]
    public async Task Get_Should_Return_NotFound_For_A_PayrollRun_Owned_By_Another_Tenant()
    {
        var (_, _, payrollRunId) = await CreateRunWithDistinctCreatorAndDryRunExecutorAsync();

        var otherTenantFinanceClient = await _factory.CreateSecondTenantFinanceAdminClientAsync();

        var response = await otherTenantFinanceClient.GetAsync($"/api/v1/finance/payroll-runs/{payrollRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "a genuinely Finance.Admin caller in a different tenant should not see this run at all");
    }

    /// <summary>Builds a second payroll run (month 6, distinct from the seeded demo run) directly
    /// through the domain — Fatima as creator, Vikram as dry-run executor — and drives it to
    /// Approved, exactly the state <c>PayrollController.Finalize</c> requires before either
    /// maker-checker check is reached.</summary>
    private async Task<(string CreatorId, string ExecutorId, Guid PayrollRunId)> CreateRunWithDistinctCreatorAndDryRunExecutorAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();

        var users = await dbContext.Set<User>().IgnoreQueryFilters().ToListAsync();
        var fatima = users.Single(u => u.Email.Value == "fatima.khan@demo.vespera.test");
        var vikram = users.Single(u => u.Email.Value == "vikram.nair@demo.vespera.test");
        var creatorId = fatima.Id.Value.ToString();
        var executorId = vikram.Id.Value.ToString();

        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(fatima.TenantId, 6, now.Year, now, creatorId).Value;
        run.FreezeAttendance(DateOnly.FromDateTime(now.UtcDateTime), freezeDay: 1, now, creatorId);
        run.RecomputeLines(
            [new PayrollLineInput(EmployeeId.New(), Money.Of(60000m, Currency.Inr), Money.Of(6000m, Currency.Inr), Money.Of(54000m, Currency.Inr), 0m)],
            executorId, now);
        run.SubmitForReview();
        run.Approve(creatorId, now);

        dbContext.Add(run);
        await dbContext.SaveChangesAsync();

        // AuditableEntityInterceptor stamps CreatedBy from the ambient system user during this
        // scope's SaveChanges, overwriting Fatima's id the same way DevelopmentSeeder's own patch
        // does for the demo run.
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"PayrollRun\" SET \"CreatedBy\" = {creatorId} WHERE \"Id\" = {run.Id.Value}");

        return (creatorId, executorId, run.Id.Value);
    }
}
