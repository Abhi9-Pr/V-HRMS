using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record CreateCorporateEventCommand(
    string Title,
    string Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string LocationText,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
