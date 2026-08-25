using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record RateTicketSatisfactionCommand(Guid TicketId, int Rating, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
