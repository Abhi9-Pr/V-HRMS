using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

/// <summary>
/// Find-or-open the <see cref="AttendanceDay"/> a new punch belongs to. Shared by the web and
/// mobile punch handlers (identical in both — the two handlers diverge on validation, not on this),
/// this is the mechanism behind "a night shift's closing punch belongs to the day the shift
/// started, not the calendar day it happens to fall on": if today's local date has no
/// <see cref="AttendanceDay"/> yet, check yesterday's before defaulting to opening a new one — if
/// yesterday's is still <see cref="AttendanceDay.IsOpen"/> (its last punch is an unmatched In), the
/// new punch is that shift's closing Out, not the start of a new day.
/// </summary>
/// <remarks>Public rather than <c>internal</c> — this codebase has no <c>InternalsVisibleTo</c>
/// wiring anywhere, so an <c>internal</c> type here would be unreachable from its own test
/// project.</remarks>
public static class AttendanceDayResolver
{
    public static async Task<(AttendanceDay Day, bool IsNew)> ResolveAsync(
        IReadRepository<AttendanceDay> attendanceDays,
        TenantId tenantId,
        EmployeeId employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var day = await attendanceDays.FirstOrDefaultAsync(
            new AttendanceDayByEmployeeAndDateSpecification(tenantId, employeeId, today), cancellationToken);
        if (day is not null)
        {
            return (day, false);
        }

        var yesterday = today.AddDays(-1);
        var priorDay = await attendanceDays.FirstOrDefaultAsync(
            new AttendanceDayByEmployeeAndDateSpecification(tenantId, employeeId, yesterday), cancellationToken);
        if (priorDay is not null && priorDay.IsOpen)
        {
            return (priorDay, false);
        }

        return (AttendanceDay.Open(tenantId, employeeId, today), true);
    }
}
