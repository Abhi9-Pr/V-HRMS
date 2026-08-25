using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.OrgChart;

/// <summary>Every non-deleted employee for the tenant, unpaged — the org chart needs the whole
/// set at once to place both managers and standalone employees with no reporting-relationship
/// row at all. Acceptable at reference-data scale; a tenant large enough for this to matter would
/// need a different (paginated/virtualized) chart strategy, out of scope here.</summary>
public sealed class EmployeesByTenantSpecification : ISpecification<Employee>
{
    public EmployeesByTenantSpecification(TenantId tenantId)
    {
        Criteria = employee => employee.TenantId == tenantId && !employee.IsDeleted;
    }

    public Expression<Func<Employee, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
