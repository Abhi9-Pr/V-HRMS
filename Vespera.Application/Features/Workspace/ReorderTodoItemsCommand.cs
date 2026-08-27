using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

/// <summary><see cref="OrderedTodoItemIds"/> is the caller's full to-do list in its new drag-drop
/// order — the same "client owns the whole arrangement" shape as
/// <c>SaveDashboardLayoutCommand</c>, not a single from/to move.</summary>
public sealed record ReorderTodoItemsCommand(IReadOnlyList<Guid> OrderedTodoItemIds) : IRequest<Result>;
