using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class EmployeeByIdSpecification : ISpecification<Employee>
{
    public EmployeeByIdSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = employee => employee.TenantId == tenantId && employee.Id == employeeId && !employee.IsDeleted;
    }

    public Expression<Func<Employee, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
