using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed record SetTodoItemUrgencyCommand(Guid TodoItemId, TodoUrgency Urgency) : IRequest<Result>;
