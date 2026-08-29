using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

/// <summary>Every department for the tenant, unpaged — backs <see cref="GetDepartmentsETagQuery"/>,
/// which needs the full matching set's RowVersions, not one page of them.</summary>
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
