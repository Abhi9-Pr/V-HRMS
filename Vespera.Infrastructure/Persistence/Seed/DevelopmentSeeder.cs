using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Authorization;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Compliance;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Identity;

namespace Vespera.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent demo-data seeder, gated by the caller (Program.cs) to Development only (and used
/// directly by <c>VesperaWebApplicationFactory</c> under the IntegrationTesting environment).
/// Idempotency is a single check against <c>Tenant.Code</c> (not tenant-scoped, so it's
/// unaffected by the ambient-tenant query filter that would otherwise hide everything from a
/// no-tenant background scope) — if the demo tenant already exists, nothing else runs. Every
/// other operation here is a write, so the tenant filter never needs to be satisfied for this to
/// work correctly.
/// </summary>
public static class DevelopmentSeeder
{
    public const string DemoTenantCode = "DEMO";

    public const string DemoPassword = "Passw0rd!23456";

    /// <summary>A fixed (not randomly generated) TOTP secret so integration tests can compute a
    /// valid code deterministically — never used outside Development/IntegrationTesting.</summary>
    public const string FinanceAdminTotpSecretBase32 = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var credentialStore = scope.ServiceProvider.GetRequiredService<IUserCredentialStore>();

        if (await dbContext.Set<Tenant>().AnyAsync(t => t.Code == DemoTenantCode, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        const string createdBy = "system";

        var tenant = Tenant.Create("Demo Company", DemoTenantCode, now, createdBy).Value;
        tenant.Reactivate(now, createdBy);
        dbContext.Add(tenant);
        var tid = tenant.Id;

        var permissions = Permissions.All
            .Select(code => Permission.Create(code, $"Grants the '{code}' capability").Value)
            .ToList();
        dbContext.AddRange(permissions);

        Permission Find(string code) => permissions.Single(p => p.Code == code);

        var adminRole = Role.Create(tid, "Admin", now, createdBy).Value;
        var hrRole = Role.Create(tid, "HR", now, createdBy).Value;
        var managerRole = Role.Create(tid, "Manager", now, createdBy).Value;
        var employeeRole = Role.Create(tid, "Employee", now, createdBy).Value;

        // Admin/SysAdmin gets every *other* permission, but deliberately not Finance.Admin — the
        // separation-of-duties proof the finance-policy-wall tests assert against. Granting
        // Finance.Admin means adding it to the dedicated Finance role below, the same way every
        // other permission is granted; it is never implied by a role name.
        foreach (var permission in permissions.Where(p => p.Code != Permissions.Finance.Admin))
        {
            adminRole.Grant(permission.Id, now, createdBy);
        }

        hrRole.Grant(Find(Permissions.Employees.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Employees.Write).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.Request).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.Approve).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Departments.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Departments.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Designations.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Designations.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Locations.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Locations.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.EmployeeDocuments.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.EmployeeDocuments.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.ReportingRelationships.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.ReportingRelationships.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.OrgChart.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Onboarding.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Onboarding.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.EmployeeImport.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Offboarding.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Offboarding.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Shifts.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Shifts.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.RotationPatterns.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.RotationPatterns.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Holidays.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Holidays.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Rosters.Read).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Rosters.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Rosters.Publish).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Attendance.ManageTeam).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Regularizations.Approve).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Regularizations.ReadTeam).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.BiometricDevices.Manage).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.ReadTeam).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.ManagePolicy).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.ManageBlackout).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.ManageDelegation).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.Encash).Id, now, createdBy);
        hrRole.Grant(Find(Permissions.Leave.Cancel).Id, now, createdBy);

        managerRole.Grant(Find(Permissions.Employees.Read).Id, now, createdBy);
        managerRole.Grant(Find(Permissions.Leave.Approve).Id, now, createdBy);
        managerRole.Grant(Find(Permissions.Leave.ReadTeam).Id, now, createdBy);
        managerRole.Grant(Find(Permissions.Leave.ManageDelegation).Id, now, createdBy);

        employeeRole.Grant(Find(Permissions.Leave.Request).Id, now, createdBy);
        employeeRole.Grant(Find(Permissions.Leave.Cancel).Id, now, createdBy);
        employeeRole.Grant(Find(Permissions.Leave.Encash).Id, now, createdBy);
        employeeRole.Grant(Find(Permissions.Regularizations.Request).Id, now, createdBy);

        var financeRole = Role.Create(tid, "Finance", now, createdBy).Value;
        financeRole.Grant(Find(Permissions.Finance.Admin).Id, now, createdBy);
        financeRole.Grant(Find(Permissions.Payroll.Read).Id, now, createdBy);
        financeRole.Grant(Find(Permissions.Payroll.Write).Id, now, createdBy);
        financeRole.Grant(Find(Permissions.Payroll.Finalize).Id, now, createdBy);

        dbContext.AddRange(adminRole, hrRole, managerRole, employeeRole, financeRole);

        var headOffice = Location.Create(
            tid, "Head Office", "1 MG Road", "Bengaluru", "India",
            GeoCoordinate.Create(12.9716, 77.5946).Value, "Asia/Kolkata", now, createdBy).Value;
        dbContext.Add(headOffice);

        var engineering = Department.Create(tid, "Engineering", "ENG", null, now, createdBy).Value;
        var humanResources = Department.Create(tid, "Human Resources", "HR", null, now, createdBy).Value;
        var sales = Department.Create(tid, "Sales", "SLS", null, now, createdBy).Value;
        var finance = Department.Create(tid, "Finance", "FIN", null, now, createdBy).Value;
        dbContext.AddRange(engineering, humanResources, sales, finance);

        var softwareEngineer = Designation.Create(tid, "Software Engineer", 3, now, createdBy).Value;
        var hrManager = Designation.Create(tid, "HR Manager", 5, now, createdBy).Value;
        var salesExecutive = Designation.Create(tid, "Sales Executive", 2, now, createdBy).Value;
        var financeManager = Designation.Create(tid, "Finance Manager", 6, now, createdBy).Value;
        dbContext.AddRange(softwareEngineer, hrManager, salesExecutive, financeManager);

        var generalShift = Shift.Create(
            tid, "General Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), graceMinutes: 10, now, createdBy).Value;
        var nightShift = Shift.Create(
            tid, "Night Shift", new TimeOnly(22, 0), new TimeOnly(6, 0), graceMinutes: 15, now, createdBy).Value;
        dbContext.AddRange(generalShift, nightShift);

        var casualLeave = LeaveType.Create(tid, "Casual Leave", isPaid: true, carryForwardLimit: 5, now, createdBy).Value;
        var sickLeave = LeaveType.Create(tid, "Sick Leave", isPaid: true, carryForwardLimit: 0, now, createdBy).Value;
        var earnedLeave = LeaveType.Create(tid, "Earned Leave", isPaid: true, carryForwardLimit: 15, now, createdBy).Value;
        earnedLeave.UpdateEligibilityRules(
            applicableGender: null, minimumTenureMonths: 0, isEncashable: true, maxEncashableDays: 10, now, createdBy);
        dbContext.AddRange(casualLeave, sickLeave, earnedLeave);

        var policyValidFrom = new DateOnly(2026, 1, 1);
        var casualPolicy = LeavePolicy.Create(
            tid, casualLeave.Id, annualEntitlementDays: 12, accrualRatePerMonth: 1, maxCarryForwardDays: 5, policyValidFrom, null).Value;
        var sickPolicy = LeavePolicy.Create(
            tid, sickLeave.Id, annualEntitlementDays: 10, accrualRatePerMonth: 0.83m, maxCarryForwardDays: 0, policyValidFrom, null).Value;
        var earnedPolicy = LeavePolicy.Create(
            tid, earnedLeave.Id, annualEntitlementDays: 18, accrualRatePerMonth: 1.5m, maxCarryForwardDays: 15, policyValidFrom, null).Value;

        // Earned Leave demonstrates the full multi-tier chain: direct manager, then (once the
        // request exceeds 3 days) the manager's manager, then a final HR sign-off.
        earnedPolicy.ConfigureApprovalChain(requiresSkipLevelApproval: false, skipLevelThresholdDays: 3m, requiresHrApproval: true);
        earnedPolicy.ConfigureBalanceRules(NegativeBalancePolicy.AllowWithLop, maxNegativeBalanceDays: 0m, sandwichLeaveEnabled: true);
        casualPolicy.ConfigureBalanceRules(NegativeBalancePolicy.AllowWithLop, maxNegativeBalanceDays: 0m, sandwichLeaveEnabled: false);
        sickPolicy.ConfigureBalanceRules(NegativeBalancePolicy.AllowNegative, maxNegativeBalanceDays: 3m, sandwichLeaveEnabled: false);

        dbContext.AddRange(casualPolicy, sickPolicy, earnedPolicy);

        dbContext.Add(BlackoutPeriod.Create(
            tid, DateRange.Create(new DateOnly(2026, 12, 24), new DateOnly(2027, 1, 2)).Value,
            "Year-end freeze", leaveTypeId: null, now, createdBy).Value);

        // Indian FY 2026-27 (1 Apr 2026 - 31 Mar 2027) statutory rates.
        var fyStart = new DateOnly(2026, 4, 1);
        var fyEnd = new DateOnly(2027, 3, 31);
        dbContext.AddRange(
            StatutoryRuleSet.Create(tid, StatutoryRuleType.ProvidentFund, ratePercent: 12m, Money.Of(1800m, Currency.Inr), fyStart, fyEnd).Value,
            StatutoryRuleSet.Create(tid, StatutoryRuleType.EmployeeStateInsurance, ratePercent: 0.75m, Money.Of(21000m, Currency.Inr), fyStart, fyEnd).Value,
            StatutoryRuleSet.Create(tid, StatutoryRuleType.ProfessionalTax, ratePercent: 0.2m, Money.Of(200m, Currency.Inr), fyStart, fyEnd).Value,
            StatutoryRuleSet.Create(tid, StatutoryRuleType.Gratuity, ratePercent: 4.81m, null, fyStart, fyEnd).Value);

        var priya = Employee.Onboard(
            tid, EmployeeCode.Create("EMP-001").Value, "Priya", "Sharma",
            EmailAddress.Create("priya.sharma@demo.vespera.test").Value, PhoneNumber.Create("+919812345001").Value,
            new DateOnly(1994, 4, 12), new DateOnly(2024, 1, 15), engineering.Id, softwareEngineer.Id, headOffice.Id, now, createdBy).Value;

        var rohan = Employee.Onboard(
            tid, EmployeeCode.Create("EMP-002").Value, "Rohan", "Verma",
            EmailAddress.Create("rohan.verma@demo.vespera.test").Value, PhoneNumber.Create("+919812345002").Value,
            new DateOnly(1988, 11, 3), new DateOnly(2023, 6, 1), humanResources.Id, hrManager.Id, headOffice.Id, now, createdBy).Value;

        var ananya = Employee.Onboard(
            tid, EmployeeCode.Create("EMP-003").Value, "Ananya", "Iyer",
            EmailAddress.Create("ananya.iyer@demo.vespera.test").Value, PhoneNumber.Create("+919812345003").Value,
            new DateOnly(1996, 7, 22), new DateOnly(2025, 3, 10), sales.Id, salesExecutive.Id, headOffice.Id, now, createdBy).Value;

        var vikram = Employee.Onboard(
            tid, EmployeeCode.Create("EMP-004").Value, "Vikram", "Nair",
            EmailAddress.Create("vikram.nair@demo.vespera.test").Value, PhoneNumber.Create("+919812345004").Value,
            new DateOnly(1985, 2, 19), new DateOnly(2022, 4, 1), finance.Id, financeManager.Id, headOffice.Id, now, createdBy).Value;

        var fatima = Employee.Onboard(
            tid, EmployeeCode.Create("EMP-005").Value, "Fatima", "Khan",
            EmailAddress.Create("fatima.khan@demo.vespera.test").Value, PhoneNumber.Create("+919812345005").Value,
            new DateOnly(1990, 9, 5), new DateOnly(2021, 1, 10), finance.Id, financeManager.Id, headOffice.Id, now, createdBy).Value;

        dbContext.AddRange(priya, rohan, ananya, vikram, fatima);

        dbContext.AddRange(
            ReportingRelationship.Create(tid, priya.Id, rohan.Id, priya.DateOfJoining, null).Value,
            ReportingRelationship.Create(tid, ananya.Id, rohan.Id, ananya.DateOfJoining, null).Value);

        var priyaUser = User.Create(tid, priya.WorkEmail, priya.Id, now, createdBy);
        priyaUser.AssignRole(employeeRole.Id, now, createdBy);

        var rohanUser = User.Create(tid, rohan.WorkEmail, rohan.Id, now, createdBy);
        rohanUser.AssignRole(hrRole.Id, now, createdBy);
        rohanUser.AssignRole(managerRole.Id, now, createdBy);

        var ananyaUser = User.Create(tid, ananya.WorkEmail, ananya.Id, now, createdBy);
        ananyaUser.AssignRole(employeeRole.Id, now, createdBy);

        // Both HR (rohanUser) and a SysAdmin below hold high-privilege roles but neither carries
        // Finance.Admin — the /api/v1/finance/* wall must still reject them.
        var sysAdminUser = User.Create(tid, EmailAddress.Create("admin@demo.vespera.test").Value, null, now, createdBy);
        sysAdminUser.AssignRole(adminRole.Id, now, createdBy);

        var vikramUser = User.Create(tid, vikram.WorkEmail, vikram.Id, now, createdBy);
        vikramUser.AssignRole(financeRole.Id, now, createdBy);

        var fatimaUser = User.Create(tid, fatima.WorkEmail, fatima.Id, now, createdBy);
        fatimaUser.AssignRole(financeRole.Id, now, createdBy);

        dbContext.AddRange(priyaUser, rohanUser, ananyaUser, sysAdminUser, vikramUser, fatimaUser);

        foreach (var user in new[] { priyaUser, rohanUser, ananyaUser, sysAdminUser, vikramUser, fatimaUser })
        {
            await credentialStore.CreateAsync(user.Id, user.Email.Value, DemoPassword, cancellationToken);
        }

        // Finance.Admin logins require TOTP (see LoginCommandHandler) — pre-enrol both finance
        // users with a known, fixed secret so integration tests can compute a valid code. The
        // ApplicationUser rows credentialStore.CreateAsync just staged aren't in the database yet
        // (TransactionBehavior-style: nothing is saved until the SaveChangesAsync below), so this
        // reads the change tracker's local set rather than issuing a query that would find nothing.
        foreach (var financeUserId in new[] { vikramUser.Id, fatimaUser.Id })
        {
            var applicationUser = dbContext.Set<ApplicationUser>().Local.Single(u => u.Id == financeUserId.Value);
            applicationUser.AuthenticatorKey = FinanceAdminTotpSecretBase32;
            applicationUser.TwoFactorEnabled = true;
        }

        // A draft payroll run "created" by Vikram — used by FinancePolicyWallTests to prove
        // maker-checker: Vikram (the creator) must not be able to finalize his own run, but
        // Fatima (a different Finance.Admin) can.
        var payrollRun = PayrollRun.Open(tid, DateTime.UtcNow.Month, DateTime.UtcNow.Year, now, vikramUser.Id.Value.ToString()).Value;
        payrollRun.AddLine(priya.Id, Money.Of(80000m, Currency.Inr), Money.Of(8000m, Currency.Inr), Money.Of(72000m, Currency.Inr), 0m);
        dbContext.Add(payrollRun);

        dbContext.AddRange(
            RetentionPolicy.Create(tid, "Employee.Document", retentionPeriodDays: 2555, RetentionAction.Anonymize, now, createdBy).Value,
            RetentionPolicy.Create(tid, "Attendance.Punch", retentionPeriodDays: 1095, RetentionAction.Purge, now, createdBy).Value,
            RetentionPolicy.Create(tid, "AuditLog", retentionPeriodDays: 2190, RetentionAction.Purge, now, createdBy).Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        // AuditableEntityInterceptor always stamps CreatedBy from the ambient ICurrentUser, which
        // is "system" during seeding (there's no HTTP request/authenticated user at startup) — it
        // overwrote the vikramUser.Id passed to PayrollRun.Open above. The maker-checker test
        // needs a real, specific creator on record, so this patches the column directly,
        // bypassing the interceptor (which only runs on tracked-entity SaveChanges, not raw SQL).
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"PayrollRun\" SET \"CreatedBy\" = {vikramUser.Id.Value.ToString()} WHERE \"Id\" = {payrollRun.Id.Value}",
            cancellationToken);
    }
}
