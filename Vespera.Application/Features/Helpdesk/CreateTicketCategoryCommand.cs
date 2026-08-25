using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record CreateTicketCategoryCommand(string Name, Guid DepartmentId, Guid? DefaultSlaPolicyId, string? IdempotencyKey)
    : IRequest<Result<Guid>>, IIdempotentRequest;
