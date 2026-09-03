using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Payroll;

/// <summary>Batch counterpart to Employees' own <c>EmployeeByIdSpecification</c> — one query for
/// every employee a payroll dry-run needs, instead of one query per employee. See
/// <see cref="RunDryRunCommandHandler"/>.</summary>
public sealed class EmployeesByIdsSpecification : ISpecification<Employee>
{
    public EmployeesByIdsSpecification(TenantId tenantId, IReadOnlyCollection<EmployeeId> employeeIds)
    {
        Criteria = employee => employee.TenantId == tenantId && employeeIds.Contains(employee.Id) && !employee.IsDeleted;
    }

    public Expression<Func<Employee, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
