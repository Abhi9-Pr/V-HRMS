using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

public sealed class LocationByIdSpecification : ISpecification<Location>
{
    public LocationByIdSpecification(TenantId tenantId, LocationId locationId)
    {
        Criteria = location => location.TenantId == tenantId && location.Id == locationId && !location.IsDeleted;
    }

    public Expression<Func<Location, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Location, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Location, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
