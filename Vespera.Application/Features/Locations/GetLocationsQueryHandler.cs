using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

public sealed class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, Result<PagedResult<LocationDto>>>
{
    private readonly IReadRepository<Location> _locations;
    private readonly ITenantContext _tenantContext;

    public GetLocationsQueryHandler(IReadRepository<Location> locations, ITenantContext tenantContext)
    {
        _locations = locations;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<LocationDto>>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
    {
        var specification = new LocationsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var locations = await _locations.ListAsync(specification, cancellationToken);
        var totalCount = await _locations.CountAsync(specification, cancellationToken);

        var items = locations.Adapt<List<LocationDto>>();

        return Result.Success(new PagedResult<LocationDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
