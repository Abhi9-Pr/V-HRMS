using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>What changed in the calling employee's own leave requests since <paramref name="Since"/>
/// — the second consumer of <see cref="DeltaSyncQueryHandlerBase{TRequest,TEntity,TDto}"/> (after
/// Attendance). Self-service only (see <see cref="GetMyLeaveDeltaSyncQueryHandler"/>'s doc
/// comment) — a mobile app syncs its own signed-in employee's leave state, never someone else's.
/// A <c>LeaveRequest</c> is never soft-deleted, only status-transitioned (Approved/Rejected/
/// Withdrawn/Cancelled), so this never reports tombstones — every changed record is an upsert
/// carrying its current status.</summary>
public sealed record GetMyLeaveDeltaSyncQuery(DateTimeOffset Since, string? Cursor, int PageSize)
    : IRequest<Result<DeltaSyncResult<LeaveRequestSyncDto>>>;

/// <summary>Compact shape for a mobile sync payload — enough for a local leave-request cache and
/// the approval-inbox screen a push deep link opens, not the full detail a web drawer would want.</summary>
public sealed record LeaveRequestSyncDto(
    Guid Id, Guid EmployeeId, Guid LeaveTypeId, DateOnly PeriodStart, DateOnly PeriodEnd, decimal RequestedDays, string Status);
