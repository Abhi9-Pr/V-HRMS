using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed record ResolvedAuthorizationClaims(IReadOnlyCollection<string> RoleNames, IReadOnlyCollection<string> PermissionCodes);

/// <summary>Resolves the role names and union of permission codes granted by a set of role ids.
/// Shared by <see cref="LoginCommandHandler"/> and <see cref="RefreshTokenCommandHandler"/> — the
/// only two places that need to bake role/permission claims into a freshly issued access token.</summary>
public sealed class PermissionResolver
{
    private readonly IReadRepository<Role> _roles;
    private readonly IReadRepository<Permission> _permissions;

    public PermissionResolver(IReadRepository<Role> roles, IReadRepository<Permission> permissions)
    {
        _roles = roles;
        _permissions = permissions;
    }

    public async Task<ResolvedAuthorizationClaims> ResolveAsync(IReadOnlyCollection<RoleId> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return new ResolvedAuthorizationClaims([], []);
        }

        var roles = await _roles.ListAsync(new RolesByIdsSpecification(roleIds), cancellationToken);
        var permissionIds = roles.SelectMany(r => r.PermissionIds).Distinct().ToList();

        if (permissionIds.Count == 0)
        {
            return new ResolvedAuthorizationClaims(roles.Select(r => r.Name).ToList(), []);
        }

        var permissions = await _permissions.ListAsync(new PermissionsByIdsSpecification(permissionIds), cancellationToken);
        return new ResolvedAuthorizationClaims(roles.Select(r => r.Name).ToList(), permissions.Select(p => p.Code).ToList());
    }
}
