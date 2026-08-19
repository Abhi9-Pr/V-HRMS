using Microsoft.AspNetCore.Authorization;

namespace Vespera.Api.Authorization;

/// <summary>"Own record vs subordinate vs any": succeeds if the resource EmployeeId is the
/// caller's own linked employee, a subordinate per the effective-dated reporting hierarchy, or
/// the caller holds <see cref="AnyOverridePermission"/> (e.g. Permissions.Employees.ReadAny).</summary>
public sealed class SubordinateOrSelfRequirement : IAuthorizationRequirement
{
    public SubordinateOrSelfRequirement(string? anyOverridePermission = null)
    {
        AnyOverridePermission = anyOverridePermission;
    }

    public string? AnyOverridePermission { get; }
}
