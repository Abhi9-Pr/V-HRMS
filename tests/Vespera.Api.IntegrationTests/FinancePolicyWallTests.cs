using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Payroll;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the /api/v1/finance wall (Finance.Admin is a real, separately-granted
/// permission, not implied by HR or SysAdmin role membership) and maker-checker (the creator of a
/// payroll run cannot finalize it themselves).</summary>
public class FinancePolicyWallTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public FinancePolicyWallTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HR_Admin_Should_Get_403_From_The_Finance_Wall()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-finance");
        var payrollRunId = await GetSeededPayrollRunIdAsync();

        var response = await client.PostAsync($"/api/v1/finance/payroll-runs/{payrollRunId}/finalize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SysAdmin_Should_Get_403_From_The_Finance_Wall()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "admin@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-admin-finance");
        var payrollRunId = await GetSeededPayrollRunIdAsync();

        var response = await client.PostAsync($"/api/v1/finance/payroll-runs/{payrollRunId}/finalize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Payroll_Run_Creator_Should_Get_403_Finalizing_Their_Own_Run()
    {
        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "vikram.nair@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-vikram-finalize", code);
        var payrollRunId = await GetSeededPayrollRunIdAsync();

        var response = await client.PostAsync($"/api/v1/finance/payroll-runs/{payrollRunId}/finalize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_Different_Finance_Admin_Should_Be_Able_To_Finalize_The_Run()
    {
        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "fatima.khan@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-fatima-finalize", code);
        var payrollRunId = await GetSeededPayrollRunIdAsync();

        var response = await client.PostAsync($"/api/v1/finance/payroll-runs/{payrollRunId}/finalize", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Get_Should_Return_NotFound_For_A_PayrollRun_Owned_By_Another_Tenant()
    {
        var payrollRunId = await GetSeededPayrollRunIdAsync();

        var otherTenantFinanceClient = await _factory.CreateSecondTenantFinanceAdminClientAsync();

        var response = await otherTenantFinanceClient.GetAsync($"/api/v1/finance/payroll-runs/{payrollRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "a genuinely Finance.Admin caller in a different tenant should not see this run at all");
    }

    private async Task<Guid> GetSeededPayrollRunIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        // IgnoreQueryFilters: this scope has no ambient HTTP tenant context to satisfy the
        // tenant global query filter — test-only code, exempt from the "only ReadRepositoryAdmin
        // calls IgnoreQueryFilters" architecture rule, which only scans the 4 solution assemblies.
        var run = await dbContext.Set<PayrollRun>().IgnoreQueryFilters().FirstAsync();
        return run.Id.Value;
    }
}
