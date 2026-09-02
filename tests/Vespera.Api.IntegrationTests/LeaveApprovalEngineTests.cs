using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>
/// End-to-end proof of the two "Done when" criteria from the Leave multi-tier approval engine
/// phase: (1) a two-tier chain routes tier one to an active proxy delegate instead of the nominal
/// manager, and (2) the ledger nets back to its pre-submission balance after an approve-then-cancel
/// cycle. Fixtures (employees, reporting relationships, leave type/policy, delegation) are built
/// directly against the DbContext rather than relying on the seeded demo org, since the seeded
/// manager (rohan.verma) is also the tenant's only HR-role user — collapsing any HR-approval tier
/// onto the same person as the direct-manager tier, which would defeat a genuine two-tier test.
/// </summary>
public sealed class LeaveApprovalEngineTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public LeaveApprovalEngineTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TwoTierApproval_With_An_Active_Proxy_On_Tier_One_Routes_Correctly()
    {
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var requester = await CreateEmployeeUserAsync(tenantId, "Requester", suffix, "Employee");
        var manager1 = await CreateEmployeeUserAsync(tenantId, "Manager1", suffix, "Manager");
        var manager2 = await CreateEmployeeUserAsync(tenantId, "Manager2", suffix, "Manager");
        var delegateEmployee = await CreateEmployeeUserAsync(tenantId, "Delegate", suffix, "Manager");

        var from = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14));
        var to = from.AddDays(1);

        Guid leaveTypeId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();

            dbContext.AddRange(
                ReportingRelationship.Create(tenantId, requester.EmployeeId, manager1.EmployeeId, new DateOnly(2020, 1, 1), null).Value,
                ReportingRelationship.Create(tenantId, manager1.EmployeeId, manager2.EmployeeId, new DateOnly(2020, 1, 1), null).Value);

            var leaveType = LeaveType.Create(tenantId, $"TwoTierLeave-{suffix}", isPaid: true, carryForwardLimit: 0, now, "test").Value;
            var leavePolicy = LeavePolicy.Create(
                tenantId, leaveType.Id, annualEntitlementDays: 12, accrualRatePerMonth: 1, maxCarryForwardDays: 0,
                new DateOnly(2020, 1, 1), null).Value;
            leavePolicy.ConfigureApprovalChain(requiresSkipLevelApproval: true, skipLevelThresholdDays: null, requiresHrApproval: false);

            // sandwichLeaveEnabled: true makes the day count equal the full calendar span
            // regardless of which weekday "today" happens to be — the routing behaviour under
            // test doesn't depend on the exact day count, so this keeps the test deterministic.
            leavePolicy.ConfigureBalanceRules(NegativeBalancePolicy.AllowNegative, maxNegativeBalanceDays: 30, sandwichLeaveEnabled: true);

            // The delegation must cover the date the *approval decision* is made (today, in this
            // test), not the leave request's own (future) dates — those are unrelated: a manager
            // delegates because they're unavailable to decide right now, regardless of when the
            // leave being decided on falls.
            var today = DateOnly.FromDateTime(now.UtcDateTime);
            var delegation = ProxyDelegation.Create(
                tenantId, manager1.EmployeeId, delegateEmployee.EmployeeId,
                DateRange.Create(today, today.AddDays(7)).Value, DelegationScope.LeaveApprovals).Value;

            dbContext.AddRange(leaveType, leavePolicy, delegation);
            await dbContext.SaveChangesAsync();

            leaveTypeId = leaveType.Id.Value;
        }

        var (requesterClient, _) = await _factory.CreateAuthenticatedClientAsync(requester.Email, DevelopmentSeeder.DemoPassword, $"device-{suffix}-req");
        var (manager1Client, _) = await _factory.CreateAuthenticatedClientAsync(manager1.Email, DevelopmentSeeder.DemoPassword, $"device-{suffix}-m1");
        var (delegateClient, _) = await _factory.CreateAuthenticatedClientAsync(delegateEmployee.Email, DevelopmentSeeder.DemoPassword, $"device-{suffix}-del");
        var (manager2Client, _) = await _factory.CreateAuthenticatedClientAsync(manager2.Email, DevelopmentSeeder.DemoPassword, $"device-{suffix}-m2");

        var submitResponse = await requesterClient.PostAsJsonAsync("/api/v1/leave/requests", new
        {
            leaveTypeId, from, to, reason = "Two-tier proxy routing test", acknowledgeInsufficientBalance = false,
        });
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, submitBody);
        var submitted = (await submitResponse.Content.ReadFromJsonAsync<SubmitResponse>())!;

        // Manager1 (the nominal tier-one approver) is NOT authorized while the delegation is active.
        var manager1AttemptResponse = await manager1Client.PostAsync($"/api/v1/leave/requests/{submitted.LeaveRequestId}/approve", null);
        manager1AttemptResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The delegate decides tier one instead.
        var delegateApproveResponse = await delegateClient.PostAsync($"/api/v1/leave/requests/{submitted.LeaveRequestId}/approve", null);
        delegateApproveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await delegateApproveResponse.Content.ReadAsStringAsync());

        // Tier two (the skip-level manager) approves next — the chain must have advanced, not completed.
        var manager2ApproveResponse = await manager2Client.PostAsync($"/api/v1/leave/requests/{submitted.LeaveRequestId}/approve", null);
        manager2ApproveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await manager2ApproveResponse.Content.ReadAsStringAsync());

        var mineResponse = await requesterClient.GetAsync("/api/v1/leave/requests/mine");
        mineResponse.EnsureSuccessStatusCode();
        var mine = await mineResponse.Content.ReadFromJsonAsync<List<LeaveRequestSummary>>();
        mine!.Single(r => r.Id == submitted.LeaveRequestId).Status.Should().Be("Approved");
    }

    [Fact]
    public async Task Ledger_Reconciles_After_An_Approve_Then_Cancel_Cycle()
    {
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var requester = await CreateEmployeeUserAsync(tenantId, "ReconRequester", suffix, "Employee");
        var manager = await CreateEmployeeUserAsync(tenantId, "ReconManager", suffix, "Manager");

        // Far enough in the future that CancelApprovedLeaveRequestCommand's "not yet started" guard passes.
        var from = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30));
        var to = from.AddDays(2);

        Guid leaveTypeId;
        Guid leaveBalanceId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();

            dbContext.Add(ReportingRelationship.Create(tenantId, requester.EmployeeId, manager.EmployeeId, new DateOnly(2020, 1, 1), null).Value);

            var leaveType = LeaveType.Create(tenantId, $"ReconLeave-{suffix}", isPaid: true, carryForwardLimit: 0, now, "test").Value;
            var leavePolicy = LeavePolicy.Create(
                tenantId, leaveType.Id, annualEntitlementDays: 12, accrualRatePerMonth: 1, maxCarryForwardDays: 0,
                new DateOnly(2020, 1, 1), null).Value;
            // sandwichLeaveEnabled: true keeps the requested-day count deterministic (equal to the
            // full calendar span) regardless of which weekday "today" happens to be at test time.
            leavePolicy.ConfigureBalanceRules(NegativeBalancePolicy.AllowWithLop, maxNegativeBalanceDays: 0, sandwichLeaveEnabled: true);

            var balance = LeaveBalance.Open(tenantId, requester.EmployeeId, leaveType.Id);
            balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Opening balance for test", now, "test");

            dbContext.AddRange(leaveType, leavePolicy, balance);
            await dbContext.SaveChangesAsync();

            leaveTypeId = leaveType.Id.Value;
            leaveBalanceId = balance.Id.Value;
        }

        var (requesterClient, _) = await _factory.CreateAuthenticatedClientAsync(requester.Email, DevelopmentSeeder.DemoPassword, $"device-{suffix}-req");
        var (managerClient, _) = await _factory.CreateAuthenticatedClientAsync(manager.Email, DevelopmentSeeder.DemoPassword, $"device-{suffix}-mgr");

        var availableBeforeSubmit = await GetAvailableAsync(leaveBalanceId);
        availableBeforeSubmit.Should().Be(10m);

        var submitResponse = await requesterClient.PostAsJsonAsync("/api/v1/leave/requests", new
        {
            leaveTypeId, from, to, reason = "Reconciliation test", acknowledgeInsufficientBalance = false,
        });
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, submitBody);
        var submitted = (await submitResponse.Content.ReadFromJsonAsync<SubmitResponse>())!;
        submitted.RequestedDays.Should().Be(3m);

        var availableAfterSubmit = await GetAvailableAsync(leaveBalanceId);
        availableAfterSubmit.Should().Be(availableBeforeSubmit - 3m, "the debit posts at submission, not at final approval");

        var approveResponse = await managerClient.PostAsync($"/api/v1/leave/requests/{submitted.LeaveRequestId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await approveResponse.Content.ReadAsStringAsync());

        var availableAfterApprove = await GetAvailableAsync(leaveBalanceId);
        availableAfterApprove.Should().Be(availableAfterSubmit, "final approval posts no further ledger movement");

        var cancelResponse = await requesterClient.PostAsJsonAsync(
            $"/api/v1/leave/requests/{submitted.LeaveRequestId}/cancel", new { reason = "Plans changed" });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await cancelResponse.Content.ReadAsStringAsync());

        var availableAfterCancel = await GetAvailableAsync(leaveBalanceId);
        availableAfterCancel.Should().Be(availableBeforeSubmit, "the reversal must net the ledger back to its pre-submission balance");

        var mineResponse = await requesterClient.GetAsync("/api/v1/leave/requests/mine");
        mineResponse.EnsureSuccessStatusCode();
        var mine = await mineResponse.Content.ReadFromJsonAsync<List<LeaveRequestSummary>>();
        mine!.Single(r => r.Id == submitted.LeaveRequestId).Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Approve_Should_Return_NotFound_For_A_Leave_Request_Owned_By_Another_Tenant()
    {
        var tenantId = new TenantId(await _factory.GetDemoTenantIdAsync());
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var requester = await CreateEmployeeUserAsync(tenantId, "IdorRequester", suffix, "Employee");
        var manager = await CreateEmployeeUserAsync(tenantId, "IdorManager", suffix, "Manager");

        var from = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14));
        var to = from.AddDays(1);

        Guid leaveTypeId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();

            dbContext.Add(ReportingRelationship.Create(tenantId, requester.EmployeeId, manager.EmployeeId, new DateOnly(2020, 1, 1), null).Value);

            var leaveType = LeaveType.Create(tenantId, $"IdorLeave-{suffix}", isPaid: true, carryForwardLimit: 0, now, "test").Value;
            var leavePolicy = LeavePolicy.Create(
                tenantId, leaveType.Id, annualEntitlementDays: 12, accrualRatePerMonth: 1, maxCarryForwardDays: 0,
                new DateOnly(2020, 1, 1), null).Value;
            leavePolicy.ConfigureBalanceRules(NegativeBalancePolicy.AllowNegative, maxNegativeBalanceDays: 30, sandwichLeaveEnabled: true);

            dbContext.AddRange(leaveType, leavePolicy);
            await dbContext.SaveChangesAsync();

            leaveTypeId = leaveType.Id.Value;
        }

        var (requesterClient, _) = await _factory.CreateAuthenticatedClientAsync(
            requester.Email, DevelopmentSeeder.DemoPassword, $"device-{suffix}-idor-req");

        var submitResponse = await requesterClient.PostAsJsonAsync("/api/v1/leave/requests", new
        {
            leaveTypeId, from, to, reason = "Cross-tenant IDOR test", acknowledgeInsufficientBalance = false,
        });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, await submitResponse.Content.ReadAsStringAsync());
        var submitted = (await submitResponse.Content.ReadFromJsonAsync<SubmitResponse>())!;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        var approveAttempt = await otherTenantClient.PostAsync($"/api/v1/leave/requests/{submitted.LeaveRequestId}/approve", null);
        approveAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<decimal> GetAvailableAsync(Guid leaveBalanceId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var balance = await dbContext.Set<LeaveBalance>().IgnoreQueryFilters()
            .Include(b => b.Entries)
            .FirstAsync(b => b.Id == new LeaveBalanceId(leaveBalanceId));
        return balance.Available;
    }

    private async Task<(EmployeeId EmployeeId, string Email)> CreateEmployeeUserAsync(
        TenantId tenantId, string label, string suffix, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var credentialStore = scope.ServiceProvider.GetRequiredService<IUserCredentialStore>();
        var now = DateTimeOffset.UtcNow;

        var email = EmailAddress.Create($"{label.ToLowerInvariant()}.{suffix}@demo.vespera.test").Value;
        var employee = Employee.Onboard(
            tenantId, EmployeeCode.Create($"T-{suffix}-{label.ToUpperInvariant()}").Value, label, suffix, email,
            PhoneNumber.Create("+919800000000").Value, new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
            DepartmentId.New(), DesignationId.New(), LocationId.New(), now, "test").Value;

        var role = await dbContext.Set<Role>().IgnoreQueryFilters().FirstAsync(r => r.TenantId == tenantId && r.Name == roleName);
        var user = User.Create(tenantId, email, employee.Id, now, "test");
        user.AssignRole(role.Id, now, "test");

        dbContext.AddRange(employee, user);
        var credentialResult = await credentialStore.CreateAsync(user.Id, email.Value, DevelopmentSeeder.DemoPassword, CancellationToken.None);
        credentialResult.IsSuccess.Should().BeTrue(credentialResult.IsFailure ? credentialResult.Error.Message : string.Empty);
        await dbContext.SaveChangesAsync();

        return (employee.Id, email.Value);
    }

    private sealed record SubmitResponse(Guid LeaveRequestId, decimal RequestedDays, decimal LossOfPayDays);

    private sealed record LeaveRequestSummary(Guid Id, string Status);
}
