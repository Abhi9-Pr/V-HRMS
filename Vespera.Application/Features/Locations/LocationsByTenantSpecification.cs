using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

/// <summary>Every location for the tenant, unpaged — backs <see cref="GetLocationsETagQuery"/>,
/// which needs the full matching set's RowVersions, not one page of them.</summary>
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
