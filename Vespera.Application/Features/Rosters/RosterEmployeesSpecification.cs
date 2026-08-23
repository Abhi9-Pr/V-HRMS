using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Rosters;

/// <summary>The employees a roster query should resolve assignments for — optionally narrowed to
/// one department or one employee; unpaged, same reference-data-scale reasoning as the org
/// chart's <c>EmployeesByTenantSpecification</c> (this is a planner view, not a paginated list).</summary>
public sealed class RosterEmployeesSpecification : ISpecification<Employee>
{
    public RosterEmployeesSpecification(TenantId tenantId, Guid? departmentId, Guid? employeeId)
    {
        // Built as one of three distinct expressions rather than a single lambda with inline
        // "param == null || comparison" clauses -- EF Core's null-semantics rewriting of that
        // pattern against a nullable-vs-non-nullable comparison here didn't translate cleanly, so
        // each case is expressed directly instead of relying on runtime short-circuiting inside
        // the expression tree. Each comparison is against the whole converted typed-id struct
        // (e.g. `employee.Id == id`), not its `.Value` member -- EF's value converter round-trips
        // the whole struct to/from the column; comparing `.Value` directly isn't translatable.
        if (employeeId is { } rawEmployeeId)
        {
            var id = new EmployeeId(rawEmployeeId);
            Criteria = employee => employee.TenantId == tenantId && !employee.IsDeleted && employee.Id == id;
        }
        else if (departmentId is { } rawDepartmentId)
        {
            var department = new DepartmentId(rawDepartmentId);
            Criteria = employee => employee.TenantId == tenantId && !employee.IsDeleted && employee.DepartmentId == department;
        }
        else
        {
            Criteria = employee => employee.TenantId == tenantId && !employee.IsDeleted;
        }
    }

    public Expression<Func<Employee, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
