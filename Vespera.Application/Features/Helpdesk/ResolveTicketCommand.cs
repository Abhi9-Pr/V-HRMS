using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record ResolveTicketCommand(Guid TicketId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
