namespace Vespera.Application.Common;

public sealed record DeltaSyncRequest(DateTimeOffset Since, string? Cursor = null, int PageSize = 100);

/// <summary>TombstonedIds lets a mobile client remove records it previously synced but that were deleted since.</summary>
public sealed record DeltaSyncResult<T>(
    IReadOnlyList<T> Upserts, IReadOnlyList<Guid> TombstonedIds, DateTimeOffset SyncedAt, string? NextCursor);
