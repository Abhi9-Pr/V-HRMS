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
    }

    public static class Leave
    {
        public const string Request = "Leave.Request";
        public const string Approve = "Leave.Approve";
    }

    public static class Departments
    {
        public const string Manage = "Departments.Manage";
    }

    public static class Expenses
    {
        public const string Submit = "Expenses.Submit";
        public const string Approve = "Expenses.Approve";
        public const string ManagePolicy = "Expenses.ManagePolicy";
        public const string Settle = "Expenses.Settle";
    }

    public static class Users
    {
        public const string Manage = "Users.Manage";
    }

    public static class Assets
    {
        public const string Read = "Assets.Read";
        public const string Write = "Assets.Write";
        public const string Assign = "Assets.Assign";
        public const string Recover = "Assets.Recover";
    }

    public static class Licenses
    {
        public const string Read = "Licenses.Read";
        public const string Manage = "Licenses.Manage";
    }

    public static class Recruitment
    {
        public const string ManageRequisitions = "Recruitment.ManageRequisitions";
        public const string ApproveRequisitions = "Recruitment.ApproveRequisitions";
        public const string ManageCandidates = "Recruitment.ManageCandidates";
        public const string ManageInterviews = "Recruitment.ManageInterviews";
        public const string ManageOffers = "Recruitment.ManageOffers";
        public const string ConvertToEmployee = "Recruitment.ConvertToEmployee";
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
