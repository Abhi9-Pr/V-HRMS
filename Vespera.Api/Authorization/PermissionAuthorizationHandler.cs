using Microsoft.AspNetCore.Authorization;
using Vespera.Infrastructure.Identity;

namespace Vespera.Api.Authorization;

/// <summary>Checks the JWT's own "permission" claims — never the database. See the Phase 4 plan:
/// permissions are resolved once at login/refresh and baked into the token, which is what makes a
/// 15-minute access token both safe and cheap to authorize against.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(JwtClaimTypes.Permission, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
