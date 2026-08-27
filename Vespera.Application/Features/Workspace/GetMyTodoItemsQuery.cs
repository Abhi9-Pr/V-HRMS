using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record GetMyTodoItemsQuery : IRequest<Result<IReadOnlyList<TodoItemDto>>>;

public sealed record TodoItemDto(Guid Id, string Title, DateOnly? DueDate, string Urgency, int SortOrder, bool IsDone);
