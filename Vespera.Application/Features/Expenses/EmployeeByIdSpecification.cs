using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Expenses;

public sealed class EmployeeByIdSpecification : ISpecification<Employee>
{
    public EmployeeByIdSpecification(EmployeeId employeeId)
    {
        Criteria = employee => employee.Id == employeeId;
    }

    public Expression<Func<Employee, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
