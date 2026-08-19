using Microsoft.AspNetCore.Authorization;

namespace Vespera.Api.Authorization;

/// <summary>
/// Permission-based authorization, not role-string-based: <c>[HasPermission(Permissions.Payroll.Finalize)]</c>.
/// Backed by <see cref="PermissionAuthorizationPolicyProvider"/> + <see cref="PermissionAuthorizationHandler"/> —
/// the standard ASP.NET Core dynamic-policy pattern for a permission whose set isn't known at
/// compile time.
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
        : base(PermissionAuthorizationPolicyProvider.PolicyName(permissionCode))
    {
    }
}
