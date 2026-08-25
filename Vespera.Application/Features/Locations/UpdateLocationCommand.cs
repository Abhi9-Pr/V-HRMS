using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Locations;

public sealed record UpdateLocationCommand(
    Guid Id,
    double Latitude,
    double Longitude,
    string AddressLine,
    string City,
    string Country) : IRequest<Result>;
