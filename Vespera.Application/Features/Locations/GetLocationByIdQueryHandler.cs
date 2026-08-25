using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

public sealed class GetLocationByIdQueryHandler : IRequestHandler<GetLocationByIdQuery, Result<LocationDto>>
{
    private readonly IReadRepository<Location> _locations;
    private readonly ITenantContext _tenantContext;

    public GetLocationByIdQueryHandler(IReadRepository<Location> locations, ITenantContext tenantContext)
    {
        _locations = locations;
        _tenantContext = tenantContext;
    }

    public async Task<Result<LocationDto>> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new LocationByIdSpecification(_tenantContext.TenantId, new LocationId(request.Id));

        var location = await _locations.FirstOrDefaultAsync(specification, cancellationToken);
        if (location is null)
        {
            return Result.Failure<LocationDto>(Error.NotFound("location.not_found", "Location not found."));
        }

        return Result.Success(location.Adapt<LocationDto>());
    }
}
