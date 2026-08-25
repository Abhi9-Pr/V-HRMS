using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.OrgChart;

/// <summary>Every non-deleted department for the tenant, unpaged — used to resolve department
/// names for org chart nodes in one bulk query instead of one lookup per node.</summary>
public sealed class DepartmentsByTenantSpecification : ISpecification<Department>
{
    public DepartmentsByTenantSpecification(TenantId tenantId)
    {
        Criteria = department => department.TenantId == tenantId && !department.IsDeleted;
    }

    public Expression<Func<Department, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Department, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Department, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
