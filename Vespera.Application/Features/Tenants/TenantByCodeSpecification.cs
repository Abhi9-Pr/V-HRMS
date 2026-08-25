using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Tenants;

public sealed class TenantByCodeSpecification : ISpecification<Tenant>
{
    public TenantByCodeSpecification(string code)
    {
        Criteria = tenant => tenant.Code == code && !tenant.IsDeleted;
    }

    public Expression<Func<Tenant, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Tenant, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Tenant, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
