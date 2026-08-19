using Vespera.Domain.Common;

namespace Vespera.Domain.ValueObjects;

public sealed class DateRange : ValueObject
{
    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return Result.Failure<DateRange>(
                Error.Validation("date_range.invalid", "End date cannot be before the start date."));
        }

        return Result.Success(new DateRange(start, end));
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start <= other.End && other.Start <= End;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
