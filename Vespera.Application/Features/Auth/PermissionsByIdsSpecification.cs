using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class PermissionsByIdsSpecification : ISpecification<Permission>
{
    public PermissionsByIdsSpecification(IReadOnlyCollection<PermissionId> permissionIds)
    {
        Criteria = permission => permissionIds.Contains(permission.Id);
    }

    public Expression<Func<Permission, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Permission, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Permission, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
