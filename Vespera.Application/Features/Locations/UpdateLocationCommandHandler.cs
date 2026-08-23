using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Locations;

public sealed class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, Result>
{
    private readonly IReadRepository<Location> _locations;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateLocationCommandHandler(
        IReadRepository<Location> locations,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _locations = locations;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var specification = new LocationByIdSpecification(_tenantContext.TenantId, new LocationId(request.Id));

        var location = await _locations.FirstOrDefaultAsync(specification, cancellationToken);
        if (location is null)
        {
            return Result.Failure(Error.NotFound("location.not_found", "Location not found."));
        }

        var coordinateResult = GeoCoordinate.Create(request.Latitude, request.Longitude);
        if (coordinateResult.IsFailure)
        {
            return Result.Failure(coordinateResult.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        return location.Relocate(coordinateResult.Value, request.AddressLine, request.City, request.Country, now, modifiedBy);
    }
}
