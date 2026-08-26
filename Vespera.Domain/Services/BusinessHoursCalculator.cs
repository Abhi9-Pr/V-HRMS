namespace Vespera.Domain.Services;

/// <summary>
/// Pure computation (no repository dependency, exactly like <see cref="ApprovalChainResolver"/>):
/// given a start instant and a duration, walks forward through business days/hours — skipping
/// weekends and whatever holiday set the caller supplies — to find the resulting due instant.
/// Callers (e.g. <c>RaiseTicketCommandHandler</c>) fetch the holiday calendar first, since Domain
/// has zero external dependencies and can't query one itself.
/// </summary>
public sealed class BusinessHoursCalculator
{
    public DateTimeOffset AddBusinessHours(
        DateTimeOffset start, TimeSpan duration, TimeOnly businessStart, TimeOnly businessEnd, IReadOnlySet<DateOnly> holidays)
    {
        var remainingMinutes = duration.TotalMinutes;
        var cursorDate = DateOnly.FromDateTime(start.DateTime);
        var cursorTime = TimeOnly.FromTimeSpan(start.TimeOfDay);

        // Roll the cursor forward onto the start of the first available business window.
        while (true)
        {
            if (!IsBusinessDay(cursorDate, holidays))
            {
                cursorDate = cursorDate.AddDays(1);
                cursorTime = businessStart;
                continue;
            }

            if (cursorTime < businessStart)
            {
                cursorTime = businessStart;
            }

            if (cursorTime >= businessEnd)
            {
                cursorDate = cursorDate.AddDays(1);
                cursorTime = businessStart;
                continue;
            }

            break;
        }

        while (remainingMinutes > 0)
        {
            if (!IsBusinessDay(cursorDate, holidays))
            {
                cursorDate = cursorDate.AddDays(1);
                cursorTime = businessStart;
                continue;
            }

            var availableTodayMinutes = (businessEnd.ToTimeSpan() - cursorTime.ToTimeSpan()).TotalMinutes;
            if (availableTodayMinutes <= 0)
            {
                cursorDate = cursorDate.AddDays(1);
                cursorTime = businessStart;
                continue;
            }

            if (remainingMinutes <= availableTodayMinutes)
            {
                cursorTime = cursorTime.Add(TimeSpan.FromMinutes(remainingMinutes));
                remainingMinutes = 0;
            }
            else
            {
                remainingMinutes -= availableTodayMinutes;
                cursorDate = cursorDate.AddDays(1);
                cursorTime = businessStart;
            }
        }

        return new DateTimeOffset(cursorDate.ToDateTime(cursorTime), start.Offset);
    }

    private static bool IsBusinessDay(DateOnly date, IReadOnlySet<DateOnly> holidays) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(date);
}
