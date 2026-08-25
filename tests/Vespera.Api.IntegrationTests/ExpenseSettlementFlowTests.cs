using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Expense;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>End-to-end proof of Phase 11a's "Done when" criterion: an expense claim opened,
/// lined, submitted, and approved through the real API can then be settled through a real
/// (Phase 10) payroll run — the claim ends up Reimbursed and the run carries a matching
/// reimbursement line.</summary>
public class ExpenseSettlementFlowTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public ExpenseSettlementFlowTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task An_Approved_Expense_Claim_Can_Be_Settled_Through_A_Payroll_Run()
    {
        // Priya (employee, EMP-001) opens and submits a claim; her seeded manager is Rohan.
        var (priyaClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-expense");

        var openResponse = await priyaClient.PostAsJsonAsync("/api/v1/expenses/claims", new { settlementCurrency = Currency.Inr });
        openResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var claimId = (await openResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var addLineResponse = await priyaClient.PostAsJsonAsync($"/api/v1/expenses/claims/{claimId}/lines", new
        {
            category = "Travel",
            amount = 1500m,
            currency = Currency.Inr,
            expenseDate = new DateOnly(2026, 1, 10),
            receiptReference = (string?)null,
            vendor = "Uber",
            taxAmount = (decimal?)null,
        });
        addLineResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var submitResponse = await priyaClient.PostAsync($"/api/v1/expenses/claims/{claimId}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Rohan (Priya's manager) approves.
        var (rohanClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-expense");
        var decisionResponse = await rohanClient.PostAsJsonAsync(
            $"/api/v1/expenses/claims/{claimId}/decision", new { approved = true, comment = "Approved" });
        decisionResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Fatima (Finance.Admin, TOTP-enrolled) opens a payroll run and settles the claim into it.
        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var (fatimaClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "fatima.khan@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-fatima-expense", code);

        var openRunResponse = await fatimaClient.PostAsJsonAsync("/api/v1/finance/payroll-runs", new { month = 2, year = 2026 });
        openRunResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payrollRunId = (await openRunResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var settleResponse = await fatimaClient.PostAsJsonAsync(
            "/api/v1/finance/expense-settlements", new { expenseClaimId = claimId, payrollRunId });
        settleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();

        var claim = await dbContext.Set<ExpenseClaim>().IgnoreQueryFilters().FirstAsync(c => c.Id == new ExpenseClaimId(claimId));
        claim.Status.Should().Be(ExpenseClaimStatus.Reimbursed);

        var payrollRun = await dbContext.Set<PayrollRun>().IgnoreQueryFilters().FirstAsync(r => r.Id == new PayrollRunId(payrollRunId));
        payrollRun.Reimbursements.Should().ContainSingle(r => r.SourceExpenseClaimId == claimId && r.Amount == Money.Of(1500m, Currency.Inr));
    }

    private sealed record IdResponse(Guid Id);
}
