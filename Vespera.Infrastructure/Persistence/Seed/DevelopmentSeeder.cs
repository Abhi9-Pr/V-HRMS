using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Compliance;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent demo-data seeder, gated by the caller (Program.cs) to Development only. Idempotency
/// is a single check against <c>Tenant.Code</c> (not tenant-scoped, so it's unaffected by the
/// ambient-tenant query filter that would otherwise hide everything from a no-tenant background
/// scope) — if the demo tenant already exists, nothing else runs. Every other operation here is a
/// write, so the tenant filter never needs to be satisfied for this to work correctly.
/// </summary>
public static class DevelopmentSeeder
{
    private const string DemoTenantCode = "DEMO";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();

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

        var permissions = new[]
        {
            Permission.Create("employees.read", "View employee records").Value,
            Permission.Create("employees.write", "Create or edit employee records").Value,
            Permission.Create("payroll.read", "View payroll data").Value,
            Permission.Create("payroll.write", "Run or edit payroll").Value,
            Permission.Create("leave.request", "Request leave").Value,
            Permission.Create("leave.approve", "Approve or reject leave requests").Value,
            Permission.Create("departments.manage", "Manage departments and designations").Value,
            Permission.Create("users.manage", "Manage users and role assignments").Value,
        };
        dbContext.AddRange(permissions);

        var adminRole = Role.Create(tid, "Admin", now, createdBy).Value;
        var hrRole = Role.Create(tid, "HR", now, createdBy).Value;
        var managerRole = Role.Create(tid, "Manager", now, createdBy).Value;
        var employeeRole = Role.Create(tid, "Employee", now, createdBy).Value;

        foreach (var permission in permissions)
        {
            adminRole.Grant(permission.Id, now, createdBy);
        }

        hrRole.Grant(permissions[0].Id, now, createdBy);
        hrRole.Grant(permissions[1].Id, now, createdBy);
        hrRole.Grant(permissions[4].Id, now, createdBy);
        hrRole.Grant(permissions[5].Id, now, createdBy);
        hrRole.Grant(permissions[6].Id, now, createdBy);

        managerRole.Grant(permissions[0].Id, now, createdBy);
        managerRole.Grant(permissions[5].Id, now, createdBy);

        employeeRole.Grant(permissions[4].Id, now, createdBy);

        dbContext.AddRange(adminRole, hrRole, managerRole, employeeRole);

        var headOffice = Location.Create(
            tid, "Head Office", "1 MG Road", "Bengaluru", "India",
            GeoCoordinate.Create(12.9716, 77.5946).Value, "Asia/Kolkata", now, createdBy).Value;
        dbContext.Add(headOffice);

        var engineering = Department.Create(tid, "Engineering", "ENG", null, now, createdBy).Value;
        var humanResources = Department.Create(tid, "Human Resources", "HR", null, now, createdBy).Value;
        var sales = Department.Create(tid, "Sales", "SLS", null, now, createdBy).Value;
        dbContext.AddRange(engineering, humanResources, sales);

        var softwareEngineer = Designation.Create(tid, "Software Engineer", 3, now, createdBy).Value;
        var hrManager = Designation.Create(tid, "HR Manager", 5, now, createdBy).Value;
        var salesExecutive = Designation.Create(tid, "Sales Executive", 2, now, createdBy).Value;
        dbContext.AddRange(softwareEngineer, hrManager, salesExecutive);

        var generalShift = Shift.Create(
            tid, "General Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), graceMinutes: 10, now, createdBy).Value;
        var nightShift = Shift.Create(
            tid, "Night Shift", new TimeOnly(22, 0), new TimeOnly(6, 0), graceMinutes: 15, now, createdBy).Value;
        dbContext.AddRange(generalShift, nightShift);

        var casualLeave = LeaveType.Create(tid, "Casual Leave", isPaid: true, carryForwardLimit: 5, now, createdBy).Value;
        var sickLeave = LeaveType.Create(tid, "Sick Leave", isPaid: true, carryForwardLimit: 0, now, createdBy).Value;
        var earnedLeave = LeaveType.Create(tid, "Earned Leave", isPaid: true, carryForwardLimit: 15, now, createdBy).Value;
        dbContext.AddRange(casualLeave, sickLeave, earnedLeave);

        var policyValidFrom = new DateOnly(2026, 1, 1);
        dbContext.AddRange(
            LeavePolicy.Create(tid, casualLeave.Id, annualEntitlementDays: 12, accrualRatePerMonth: 1, maxCarryForwardDays: 5, policyValidFrom, null).Value,
            LeavePolicy.Create(tid, sickLeave.Id, annualEntitlementDays: 10, accrualRatePerMonth: 0.83m, maxCarryForwardDays: 0, policyValidFrom, null).Value,
            LeavePolicy.Create(tid, earnedLeave.Id, annualEntitlementDays: 18, accrualRatePerMonth: 1.5m, maxCarryForwardDays: 15, policyValidFrom, null).Value);

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

        dbContext.AddRange(priya, rohan, ananya);

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

        dbContext.AddRange(priyaUser, rohanUser, ananyaUser);

        dbContext.AddRange(
            RetentionPolicy.Create(tid, "Employee.Document", retentionPeriodDays: 2555, RetentionAction.Anonymize, now, createdBy).Value,
            RetentionPolicy.Create(tid, "Attendance.Punch", retentionPeriodDays: 1095, RetentionAction.Purge, now, createdBy).Value,
            RetentionPolicy.Create(tid, "AuditLog", retentionPeriodDays: 2190, RetentionAction.Purge, now, createdBy).Value);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
