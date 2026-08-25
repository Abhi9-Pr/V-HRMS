using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Locations;

public sealed record CreateLocationCommand(
    string Name,
    string AddressLine,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string TimeZoneId,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
