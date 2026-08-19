using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Vespera.Domain.Common;

namespace Vespera.Api.Authorization;

/// <summary>Resource-based maker-checker: succeeds when the authenticated user's id does not
/// match the resource's <c>CreatedBy</c>. <c>CreatedBy</c> is stamped from
/// <c>ICurrentUser.UserId?.ToString()</c> at creation time (see AuditableEntityInterceptor /
/// every command handler's <c>createdBy</c> argument), so the comparison is against the same
/// string shape the JWT's NameIdentifier claim carries.</summary>
public sealed class NotCreatorAuthorizationHandler : AuthorizationHandler<NotCreatorRequirement, IAuditable>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NotCreatorRequirement requirement, IAuditable resource)
    {
        var currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId is not null && !string.Equals(resource.CreatedBy, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
