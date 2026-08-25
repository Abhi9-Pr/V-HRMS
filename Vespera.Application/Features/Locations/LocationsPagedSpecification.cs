using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

public sealed class LocationsPagedSpecification : ISpecification<Location>
{
    public LocationsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = location => location.TenantId == tenantId && !location.IsDeleted;
        OrderBy = [(location => (object)location.Name, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Location, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Location, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Location, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
