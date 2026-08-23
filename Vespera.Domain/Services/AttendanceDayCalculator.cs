using Vespera.Domain.Attendance;

namespace Vespera.Domain.Services;

/// <summary>The computed shape of one attendance day, produced by <see cref="AttendanceDayCalculator"/>
/// and applied to an <see cref="AttendanceDay"/> via <c>AttendanceDay.ApplyComputation</c>.</summary>
public sealed record AttendanceComputationResult(
    DateTimeOffset? FirstIn,
    DateTimeOffset? LastOut,
    int WorkedMinutes,
    int LateByMinutes,
    int EarlyLeaveByMinutes,
    int OvertimeMinutes,
    bool IsLopCandidate,
    AttendanceDayStatus Status);

/// <summary>
/// Turns a day's raw punches into a computed result — first-in/last-out, break-deducted worked
/// time, late/early-leave/overtime relative to the assigned shift, and whether the day is a
/// loss-of-pay candidate. Pure: no I/O, no repository access, no timezone conversion.
/// <para>
/// Two design points worth being explicit about:
/// </para>
/// <para>
/// <b>No date-boundary logic here.</b> Punches are paired strictly by chronological order within
/// whatever list the caller hands in — this is what makes a 22:00→06:00 night shift's pair work
/// correctly, as long as the *caller* already gathered the right punches into one
/// <see cref="AttendanceDay"/>'s set at ingestion time (see
/// <c>RecordWebPunchCommandHandler</c>/<c>RecordMobilePunchCommandHandler</c>'s open-yesterday's-day
/// check). This calculator never looks at calendar dates at all.
/// </para>
/// <para>
/// <b>No direct timezone awareness.</b> <see cref="Domain.Attendance.Shift.StartTime"/>/<see cref="Domain.Attendance.Shift.EndTime"/>
/// are local <see cref="TimeOnly"/> values with no zone attached, and <see cref="Domain"/> cannot
/// reference a timezone library (zero third-party packages, enforced by
/// <c>DependencyRuleTests</c>) or the <c>ITimeZoneConverter</c> port (that's an Application
/// abstraction). So late/early/overtime comparisons take the caller's already-resolved local
/// time-of-day for the first in-punch and last out-punch (<paramref name="firstInLocalTime"/>/
/// <paramref name="lastOutLocalTime"/> on <see cref="Compute"/>) rather than converting anything
/// itself — the Application-layer caller does that one conversion via
/// <c>ITimeZoneConverter.ToZoned</c> before calling in.
/// </para>
/// </summary>
public static class AttendanceDayCalculator
{
    /// <summary>A day is a half-day when worked time is under half the shift's scheduled
    /// duration — an arbitrary but explicit threshold, not specified by the requirement.</summary>
    private const double HalfDayThreshold = 0.5;

    public static AttendanceComputationResult Compute(
        IReadOnlyList<AttendancePunch> punches,
        Shift? assignedShift,
        bool isHoliday,
        bool isWeekOff,
        TimeOnly? firstInLocalTime,
        TimeOnly? lastOutLocalTime)
    {
        var ordered = punches.OrderBy(p => p.PunchedAtUtc).ToList();

        DateTimeOffset? firstIn = null;
        DateTimeOffset? lastOut = null;
        DateTimeOffset? openIn = null;
        var rawWorkedMinutes = 0;

        foreach (var punch in ordered)
        {
            if (punch.PunchType == PunchType.In)
            {
                firstIn ??= punch.PunchedAtUtc;
                openIn = punch.PunchedAtUtc;
            }
            else if (openIn is { } start)
            {
                rawWorkedMinutes += (int)(punch.PunchedAtUtc - start).TotalMinutes;
                lastOut = punch.PunchedAtUtc;
                openIn = null;
            }
        }

        var breakMinutes = assignedShift?.BreakMinutes ?? 0;
        var workedMinutes = Math.Max(0, rawWorkedMinutes - breakMinutes);

        var lateByMinutes = 0;
        var earlyLeaveByMinutes = 0;
        var overtimeMinutes = 0;
        var scheduledMinutes = 0;

        if (assignedShift is not null)
        {
            scheduledMinutes = ScheduledDurationMinutes(assignedShift);

            if (firstInLocalTime is { } actualIn)
            {
                var graceDeadline = assignedShift.StartTime.Add(TimeSpan.FromMinutes(assignedShift.GraceMinutes));
                if (actualIn > graceDeadline)
                {
                    lateByMinutes = (int)(actualIn - assignedShift.StartTime).TotalMinutes;
                }
            }

            if (lastOutLocalTime is { } actualOut)
            {
                if (actualOut < assignedShift.EndTime)
                {
                    earlyLeaveByMinutes = (int)(assignedShift.EndTime - actualOut).TotalMinutes;
                }
                else if (actualOut > assignedShift.EndTime)
                {
                    overtimeMinutes = (int)(actualOut - assignedShift.EndTime).TotalMinutes;
                }
            }
        }

        var isLopCandidate = ordered.Count == 0 && !isHoliday && !isWeekOff && assignedShift is not null;

        var status = ResolveStatus(ordered.Count, isHoliday, isWeekOff, assignedShift, workedMinutes, scheduledMinutes);

        return new AttendanceComputationResult(
            firstIn, lastOut, workedMinutes, lateByMinutes, earlyLeaveByMinutes, overtimeMinutes, isLopCandidate, status);
    }

    private static AttendanceDayStatus ResolveStatus(
        int punchCount, bool isHoliday, bool isWeekOff, Shift? assignedShift, int workedMinutes, int scheduledMinutes)
    {
        if (isHoliday)
        {
            return AttendanceDayStatus.Holiday;
        }

        if (isWeekOff)
        {
            return AttendanceDayStatus.WeekOff;
        }

        if (punchCount == 0)
        {
            return AttendanceDayStatus.Absent;
        }

        if (assignedShift is not null && scheduledMinutes > 0 && workedMinutes < scheduledMinutes * HalfDayThreshold)
        {
            return AttendanceDayStatus.HalfDay;
        }

        return AttendanceDayStatus.Present;
    }

    /// <summary>The shift's scheduled duration in minutes, handling the overnight case (EndTime
    /// earlier in the clock than StartTime means the shift crosses midnight).</summary>
    private static int ScheduledDurationMinutes(Shift shift)
    {
        var start = shift.StartTime.ToTimeSpan();
        var end = shift.EndTime.ToTimeSpan();
        var duration = shift.IsOvernight ? (TimeSpan.FromHours(24) - start) + end : end - start;
        return (int)duration.TotalMinutes;
    }
}
