using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record SetTodoItemDoneCommand(Guid TodoItemId, bool Done) : IRequest<Result>;
