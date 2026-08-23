using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class DepartmentByIdSpecification : ISpecification<Department>
{
    public DepartmentByIdSpecification(TenantId tenantId, DepartmentId departmentId)
    {
        Criteria = department => department.TenantId == tenantId && department.Id == departmentId && !department.IsDeleted;
    }

    public Expression<Func<Department, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Department, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Department, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
