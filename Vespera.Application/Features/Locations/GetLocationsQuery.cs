using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Locations;

public sealed record GetLocationsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<LocationDto>>>;

public sealed record LocationDto(
    Guid Id,
    string Name,
    string AddressLine,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string TimeZoneId);

/// <summary>Compact profile for mobile list responses — ID plus display-critical fields only.</summary>
public sealed record LocationSummaryDto(Guid Id, string Name);
