using System.Reflection;

namespace Vespera.Application.Authorization;

/// <summary>
/// The permission-code catalog. Every <c>[HasPermission(...)]</c> check and every
/// <c>Permission</c> row the seeder creates references one of these constants — never a bare
/// string — so a typo fails to compile instead of silently granting nothing.
/// </summary>
public static class Permissions
{
    public static class Employees
    {
        public const string Read = "Employees.Read";
        public const string Write = "Employees.Write";

        /// <summary>Override for "any employee", not just self/subordinates — see
        /// <c>SubordinateOrSelfRequirement</c>.</summary>
        public const string ReadAny = "Employees.ReadAny";
    }

    public static class Payroll
    {
        public const string Read = "Payroll.Read";
        public const string Write = "Payroll.Write";
        public const string Finalize = "Payroll.Finalize";

        /// <summary>Every employee's own investment declaration (submit, add a line) - distinct
        /// from <see cref="Read"/>/<see cref="Write"/>, which are Finance-only and cover every
        /// employee's payroll data, not just the caller's own.</summary>
        public const string SelfService = "Payroll.SelfService";
    }

    public static class Leave
    {
        public const string Request = "Leave.Request";
        public const string Approve = "Leave.Approve";
        public const string Cancel = "Leave.Cancel";
        public const string Encash = "Leave.Encash";
        public const string ReadTeam = "Leave.ReadTeam";
        public const string ManagePolicy = "Leave.ManagePolicy";
        public const string ManageBlackout = "Leave.ManageBlackout";
        public const string ManageDelegation = "Leave.ManageDelegation";
    }

    public static class Departments
    {
        public const string Read = "Departments.Read";
        public const string Manage = "Departments.Manage";
    }

    public static class Designations
    {
        public const string Read = "Designations.Read";
        public const string Manage = "Designations.Manage";
    }

    public static class Locations
    {
        public const string Read = "Locations.Read";
        public const string Manage = "Locations.Manage";
    }

    public static class EmployeeDocuments
    {
        public const string Read = "EmployeeDocuments.Read";
        public const string Manage = "EmployeeDocuments.Manage";

        /// <summary>Reveals a masked PII field (PAN, bank account, compensation). Distinct from
        /// <c>Read</c> so viewing a masked profile doesn't imply the right to unmask it — every
        /// use writes a <see cref="Vespera.Application.Abstractions.Services.IPiiAccessAuditor"/> entry.</summary>
        public const string Unmask = "EmployeeDocuments.Unmask";
    }

    public static class ReportingRelationships
    {
        public const string Read = "ReportingRelationships.Read";
        public const string Manage = "ReportingRelationships.Manage";
    }

    public static class OrgChart
    {
        public const string Read = "OrgChart.Read";
    }

    public static class Onboarding
    {
        public const string Read = "Onboarding.Read";
        public const string Manage = "Onboarding.Manage";
    }

    public static class EmployeeImport
    {
        public const string Manage = "EmployeeImport.Manage";
    }

    public static class Offboarding
    {
        public const string Read = "Offboarding.Read";
        public const string Manage = "Offboarding.Manage";
    }

    public static class Shifts
    {
        public const string Read = "Shifts.Read";
        public const string Manage = "Shifts.Manage";
    }

    public static class RotationPatterns
    {
        public const string Read = "RotationPatterns.Read";
        public const string Manage = "RotationPatterns.Manage";
    }

    public static class Rosters
    {
        public const string Read = "Rosters.Read";
        public const string Manage = "Rosters.Manage";
        public const string Publish = "Rosters.Publish";
    }

    public static class Attendance
    {
        public const string Read = "Attendance.Read";
        public const string ReadTeam = "Attendance.ReadTeam";
        public const string ManageTeam = "Attendance.ManageTeam";
    }

    public static class Regularizations
    {
        public const string Request = "Regularizations.Request";
        public const string Approve = "Regularizations.Approve";
        public const string ReadTeam = "Regularizations.ReadTeam";
    }

    public static class Holidays
    {
        public const string Read = "Holidays.Read";
        public const string Manage = "Holidays.Manage";
    }

    public static class BiometricDevices
    {
        public const string Manage = "BiometricDevices.Manage";
    }

    public static class Users
    {
        public const string Manage = "Users.Manage";
    }

    /// <summary>The separation-of-duties permission behind the finance policy wall
    /// (<c>/api/v1/finance/*</c>). Deliberately not implied by any role name — see
    /// docs/data-protection.md-adjacent reasoning in the Phase 4 plan: granting it means adding it
    /// to a role explicitly, the same way every other permission is granted.</summary>
    public static class Finance
    {
        public const string Admin = "Finance.Admin";
    }

    /// <summary>Every permission code in the catalog, discovered by reflection so the seeder can
    /// never drift from the constants actually referenced in code.</summary>
    public static IReadOnlyList<string> All { get; } = typeof(Permissions)
        .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();
}
