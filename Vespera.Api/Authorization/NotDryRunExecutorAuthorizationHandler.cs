using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Vespera.Domain.Payroll;

namespace Vespera.Api.Authorization;

/// <summary>Succeeds when the authenticated user's id does not match the payroll run's
/// <see cref="PayrollRun.DryRunExecutedBy"/> — or when the run has no recorded dry-run executor yet
/// (nothing to block against, and <c>PayrollRun.Finalize</c> would already refuse a run that never
/// reached Approved anyway).</summary>
public sealed class NotDryRunExecutorAuthorizationHandler : AuthorizationHandler<NotDryRunExecutorRequirement, PayrollRun>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NotDryRunExecutorRequirement requirement, PayrollRun resource)
    {
        var currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (resource.DryRunExecutedBy is null ||
            (currentUserId is not null && !string.Equals(resource.DryRunExecutedBy, currentUserId, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
