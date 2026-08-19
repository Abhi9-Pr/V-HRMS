using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class RolesByIdsSpecification : ISpecification<Role>
{
    public RolesByIdsSpecification(IReadOnlyCollection<RoleId> roleIds)
    {
        Criteria = role => roleIds.Contains(role.Id);
    }

    public Expression<Func<Role, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Role, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Role, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
