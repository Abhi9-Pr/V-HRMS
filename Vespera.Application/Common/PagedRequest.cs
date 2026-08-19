namespace Vespera.Application.Common;

public sealed record PagedRequest(int Page = 1, int PageSize = 20, string? SortBy = null, bool SortDescending = false);
