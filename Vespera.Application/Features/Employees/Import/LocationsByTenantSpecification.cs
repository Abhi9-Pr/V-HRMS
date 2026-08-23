using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees.Import;

/// <summary>Every non-deleted location for the tenant, unpaged — used to resolve location names
/// to ids in one bulk query instead of one lookup per import row. Mirrors
/// <c>Vespera.Application.Features.OrgChart.DepartmentsByTenantSpecification</c>'s shape.</summary>
public sealed class LocationsByTenantSpecification : ISpecification<Location>
{
    public LocationsByTenantSpecification(TenantId tenantId)
    {
        Criteria = location => location.TenantId == tenantId && !location.IsDeleted;
    }

    public Expression<Func<Location, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Location, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Location, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
