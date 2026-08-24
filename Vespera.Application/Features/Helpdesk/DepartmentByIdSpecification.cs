using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Helpdesk;

public sealed class DepartmentByIdSpecification : ISpecification<Department>
{
    public DepartmentByIdSpecification(DepartmentId departmentId)
    {
        Criteria = department => department.Id == departmentId;
    }

    public Expression<Func<Department, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Department, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Department, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
