using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Leave;

public sealed class RoleByTenantAndNameSpecification : ISpecification<Role>
{
    public RoleByTenantAndNameSpecification(TenantId tenantId, string name)
    {
        Criteria = r => r.TenantId == tenantId && r.Name == name;
    }

    public Expression<Func<Role, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Role, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Role, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
