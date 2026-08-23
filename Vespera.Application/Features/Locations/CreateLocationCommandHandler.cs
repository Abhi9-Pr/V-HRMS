using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Locations;

public sealed class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, Result<Guid>>
{
    private readonly IWriteRepository<Location> _locations;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateLocationCommandHandler(
        IWriteRepository<Location> locations,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _locations = locations;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var coordinateResult = GeoCoordinate.Create(request.Latitude, request.Longitude);
        if (coordinateResult.IsFailure)
        {
            return Result.Failure<Guid>(coordinateResult.Error);
        }

        var result = Location.Create(
            _tenantContext.TenantId,
            request.Name,
            request.AddressLine,
            request.City,
            request.Country,
            coordinateResult.Value,
            request.TimeZoneId,
            _dateTimeProvider.UtcNow,
            _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _locations.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
