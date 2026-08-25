using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

/// <summary>Recomputes an employee's <c>AttendanceDay</c> state for every date in the given
/// range. The same handler serves both the nightly sweep (<c>AttendanceDayComputationHostedService</c>)
/// and an on-demand HR-triggered recompute — no special-casing, just two different callers.</summary>
public sealed record RecomputeAttendanceDayCommand(Guid EmployeeId, DateOnly RangeStart, DateOnly RangeEnd) : IRequest<Result>;
