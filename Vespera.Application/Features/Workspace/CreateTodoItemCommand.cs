using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed record CreateTodoItemCommand(
    string Title, DateOnly? DueDate, TodoUrgency Urgency, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
