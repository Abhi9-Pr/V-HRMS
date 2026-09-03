using FluentAssertions;
using Vespera.Domain.Services;

namespace Vespera.Domain.UnitTests.Services;

public class BusinessHoursCalculatorTests
{
    private static readonly TimeOnly BusinessStart = new(9, 0);
    private static readonly TimeOnly BusinessEnd = new(18, 0);
    private static readonly BusinessHoursCalculator Calculator = new();

    [Fact]
    public void AddBusinessHours_Should_Fit_Entirely_Within_The_Start_Days_Remaining_Window()
    {
        // Monday 2026-01-05 10:00 + 3h, well inside the 09:00-18:00 window -> same-day 13:00.
        var start = new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

        var due = Calculator.AddBusinessHours(start, TimeSpan.FromHours(3), BusinessStart, BusinessEnd, new HashSet<DateOnly>());

        due.Should().Be(new DateTimeOffset(2026, 1, 5, 13, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void AddBusinessHours_Should_Skip_The_Weekend()
    {
        // Friday 2026-01-02 16:00 + 3h: 2h left Friday (-> 18:00), 1h remaining rolls past
        // Sat/Sun straight to Monday 2026-01-05 09:00 -> 10:00.
        var start = new DateTimeOffset(2026, 1, 2, 16, 0, 0, TimeSpan.Zero);

        var due = Calculator.AddBusinessHours(start, TimeSpan.FromHours(3), BusinessStart, BusinessEnd, new HashSet<DateOnly>());

        due.Should().Be(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void AddBusinessHours_Should_Skip_A_Configured_Holiday()
    {
        // Thursday 2026-01-01 16:00 + 3h: 2h left Thursday, 1h remaining. Friday 2026-01-02 is a
        // configured holiday (skipped entirely, not just the weekend), landing on Monday 09:00 -> 10:00.
        var start = new DateTimeOffset(2026, 1, 1, 16, 0, 0, TimeSpan.Zero);
        var holidays = new HashSet<DateOnly> { new(2026, 1, 2) };

        var due = Calculator.AddBusinessHours(start, TimeSpan.FromHours(3), BusinessStart, BusinessEnd, holidays);

        due.Should().Be(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void AddBusinessHours_Should_Roll_A_Start_Outside_Business_Hours_To_The_Next_Opening()
    {
        // Thursday 2026-01-01 20:00 (past close) + 1h rolls to Friday 2026-01-02 09:00 -> 10:00,
        // consuming none of the duration on the 20:00-24:00 stretch.
        var start = new DateTimeOffset(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

        var due = Calculator.AddBusinessHours(start, TimeSpan.FromHours(1), BusinessStart, BusinessEnd, new HashSet<DateOnly>());

        due.Should().Be(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void AddBusinessHours_Should_Roll_A_Start_Before_The_Business_Day_Opens_Forward_To_Opening()
    {
        // Monday 2026-01-05 07:00 (before 09:00 open) + 1h -> same-day 09:00 -> 10:00.
        var start = new DateTimeOffset(2026, 1, 5, 7, 0, 0, TimeSpan.Zero);

        var due = Calculator.AddBusinessHours(start, TimeSpan.FromHours(1), BusinessStart, BusinessEnd, new HashSet<DateOnly>());

        due.Should().Be(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void AddBusinessHours_Should_Roll_A_Weekend_Start_Forward_To_The_Next_Business_Day()
    {
        // Saturday 2026-01-03 10:00 + 1h rolls straight to Monday 2026-01-05 09:00 -> 10:00.
        var start = new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero);

        var due = Calculator.AddBusinessHours(start, TimeSpan.FromHours(1), BusinessStart, BusinessEnd, new HashSet<DateOnly>());

        due.Should().Be(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void AddBusinessHours_Should_Span_Multiple_Full_Business_Days()
    {
        // Monday 2026-01-05 09:00 + 27h (9h/day window): rolls over Mon->Tue->Wed (9h consumed
        // each of the first two days), landing exactly at Wednesday 2026-01-07 close (18:00).
        var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        var due = Calculator.AddBusinessHours(start, TimeSpan.FromHours(27), BusinessStart, BusinessEnd, new HashSet<DateOnly>());

        due.Should().Be(new DateTimeOffset(2026, 1, 7, 18, 0, 0, TimeSpan.Zero));
    }
}
