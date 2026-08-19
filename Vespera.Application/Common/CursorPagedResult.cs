namespace Vespera.Application.Common;

/// <summary>Cursor-based paging, used instead of PagedResult&lt;T&gt; wherever the mobile app is the consumer.</summary>
public sealed record CursorPagedResult<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore);
