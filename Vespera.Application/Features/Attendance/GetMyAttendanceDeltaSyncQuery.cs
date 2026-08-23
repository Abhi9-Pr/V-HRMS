using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

/// <summary>What changed in the calling employee's own attendance since <paramref name="Since"/>
/// — the first real consumer of <see cref="DeltaSyncQueryHandlerBase{TRequest,TEntity,TDto}"/>.
/// Self-service only (see <see cref="GetMyAttendanceDeltaSyncQueryHandler"/>'s doc comment) — a
/// mobile app syncs its own signed-in employee's data, never someone else's, so no separate
/// permission beyond authentication is required.</summary>
public sealed record GetMyAttendanceDeltaSyncQuery(DateTimeOffset Since, string? Cursor, int PageSize)
    : IRequest<Result<DeltaSyncResult<AttendanceDaySummaryDto>>>;

/// <summary>Compact shape for a mobile sync payload — not the full detail a web "punch detail
/// drawer" would want, just enough for a local calendar cache.</summary>
public sealed record AttendanceDaySummaryDto(
    Guid Id, DateOnly Date, string Status, int WorkedMinutes, bool RequiresApproval);
