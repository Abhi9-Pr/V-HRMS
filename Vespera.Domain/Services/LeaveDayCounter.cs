using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Services;

/// <summary>Turns a requested <see cref="DateRange"/> into a day count, excluding weekends and
/// holidays — unless the sandwich-leave rule applies. With the rule off, a weekend or holiday that
/// falls inside the range is not charged as leave (e.g. Fri + Mon requested around an ordinary
/// weekend charges 2 days). With the rule on, every calendar day in the range is charged — a
/// weekend/holiday "sandwiched" between two requested leave days is treated as leave too, exactly
/// like a leading/trailing one within the same continuous request.</summary>
public sealed class LeaveDayCounter
{
    public decimal CountDays(DateRange period, IReadOnlySet<DateOnly> holidays, bool sandwichRuleEnabled)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(holidays);

        if (sandwichRuleEnabled)
        {
            return period.TotalDays;
        }

        var count = 0;
        for (var date = period.Start; date <= period.End; date = date.AddDays(1))
        {
            if (!IsWeekend(date) && !holidays.Contains(date))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
