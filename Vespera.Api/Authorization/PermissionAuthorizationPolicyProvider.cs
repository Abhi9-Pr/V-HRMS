using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Vespera.Api.Authorization;

/// <summary>Recognizes the "Permission:{code}" policy-name prefix and builds a policy requiring
/// that permission on the fly — there's no way to pre-register one named policy per catalog
/// entry, since the catalog can grow. Falls back to <see cref="DefaultAuthorizationPolicyProvider"/>
/// for every other (explicitly registered) policy name, e.g. the finance wall's own policy.</summary>
public sealed class PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "Permission:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public static string PolicyName(string permissionCode) => PolicyPrefix + permissionCode;

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            var permissionCode = policyName[PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionCode))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
