using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record AssignTicketCommand(Guid TicketId, Guid EmployeeId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
