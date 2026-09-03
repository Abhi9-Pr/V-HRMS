using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

/// <summary>Attendance status for many employees over a date range — the "team attendance" view
/// this HRMS didn't have a backend for until now. Bounded two ways: RangeStart/RangeEnd must span
/// at most 31 days (validator), and the employee set itself is paged — this can never turn into an
/// unbounded scan.</summary>
public sealed record GetAttendanceGridQuery(
    Guid? DepartmentId, Guid? LocationId, DateOnly RangeStart, DateOnly RangeEnd, PagedRequest Paging)
    : IRequest<Result<PagedResult<AttendanceGridRowDto>>>;

/// <summary>Flat projection off <c>IVesperaDbContext.Set&lt;Employee&gt;()</c> — see
/// docs/CONTRIBUTING-slices.md's guidance on when to reach for this instead of
/// IReadRepository+specification. Not client-facing; the handler regroups these into
/// <see cref="AttendanceGridRowDto"/>.</summary>
public sealed record AttendanceGridEmployeeProjection(Guid EmployeeId, string EmployeeCode, string FirstName, string LastName);

/// <summary>Flat projection off <c>IVesperaDbContext.Set&lt;AttendanceDay&gt;()</c> — one row per
/// employee per day that actually has a computed <c>AttendanceDay</c>. A date with no row (not yet
/// computed, or before the employee joined) simply has no entry — the handler doesn't invent a
/// synthetic "no data" status.</summary>
public sealed record AttendanceGridCellProjection(Guid EmployeeId, DateOnly Date, string Status);

public sealed record AttendanceGridDayDto(DateOnly Date, string? Status);

public sealed record AttendanceGridRowDto(Guid EmployeeId, string EmployeeCode, string EmployeeName, IReadOnlyList<AttendanceGridDayDto> Days);
