using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class EmployeesPagedSpecification : ISpecification<Employee>
{
    public EmployeesPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = employee => employee.TenantId == tenantId && !employee.IsDeleted;
        OrderBy = [(employee => (object)employee.LastName, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Employee, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
