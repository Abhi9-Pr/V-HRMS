using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Payroll;

public sealed class TenantByIdSpecification : ISpecification<Tenant>
{
    public TenantByIdSpecification(TenantId tenantId)
    {
        Criteria = tenant => tenant.Id == tenantId;
    }

    public Expression<Func<Tenant, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Tenant, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Tenant, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
