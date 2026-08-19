using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class DepartmentsPagedSpecification : ISpecification<Department>
{
    public DepartmentsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = department => department.TenantId == tenantId && !department.IsDeleted;
        OrderBy = [(department => (object)department.Name, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Department, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Department, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Department, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
