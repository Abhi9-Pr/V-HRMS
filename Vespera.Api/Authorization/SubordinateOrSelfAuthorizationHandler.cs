using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Infrastructure.Identity;

namespace Vespera.Api.Authorization;

public sealed class SubordinateOrSelfAuthorizationHandler : AuthorizationHandler<SubordinateOrSelfRequirement, EmployeeId>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SubordinateOrSelfAuthorizationHandler(
        IReadRepository<User> users, IReadRepository<ReportingRelationship> reportingRelationships, IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _reportingRelationships = reportingRelationships;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SubordinateOrSelfRequirement requirement, EmployeeId resource)
    {
        if (requirement.AnyOverridePermission is not null && context.User.HasClaim(JwtClaimTypes.Permission, requirement.AnyOverridePermission))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userIdValue))
        {
            return;
        }

        var user = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), CancellationToken.None);
        if (user?.EmployeeId is not { } callerEmployeeId)
        {
            return;
        }

        if (callerEmployeeId == resource)
        {
            context.Succeed(requirement);
            return;
        }

        var asOf = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var isSubordinate = await _reportingRelationships.AnyAsync(
            new ActiveSubordinateSpecification(callerEmployeeId, resource, asOf), CancellationToken.None);

        if (isSubordinate)
        {
            context.Succeed(requirement);
        }
    }
}
