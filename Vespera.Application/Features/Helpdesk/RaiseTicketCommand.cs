using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed record RaiseTicketCommand(Guid CategoryId, string Subject, string Description, TicketPriority Priority, string? IdempotencyKey)
    : IRequest<Result<Guid>>, IIdempotentRequest;
