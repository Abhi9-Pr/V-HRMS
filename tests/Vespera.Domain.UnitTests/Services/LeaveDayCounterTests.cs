using FluentAssertions;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Services;

public class LeaveDayCounterTests
{
    private readonly LeaveDayCounter _counter = new();

    [Fact]
    public void CountDays_Should_Exclude_Weekends_When_The_Sandwich_Rule_Is_Off()
    {
        // Mon 2026-03-02 .. Fri 2026-03-06: a plain 5-weekday week, no weekend inside it.
        var period = DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6)).Value;

        _counter.CountDays(period, holidays: new HashSet<DateOnly>(), sandwichRuleEnabled: false).Should().Be(5m);
    }

    [Fact]
    public void CountDays_Should_Skip_A_Weekend_Bridged_By_The_Request_When_The_Sandwich_Rule_Is_Off()
    {
        // Fri 2026-03-06 .. Mon 2026-03-09: bridges Sat/Sun.
        var period = DateRange.Create(new DateOnly(2026, 3, 6), new DateOnly(2026, 3, 9)).Value;

        _counter.CountDays(period, holidays: new HashSet<DateOnly>(), sandwichRuleEnabled: false).Should().Be(2m);
    }

    [Fact]
    public void CountDays_Should_Charge_The_Bridged_Weekend_When_The_Sandwich_Rule_Is_On()
    {
        var period = DateRange.Create(new DateOnly(2026, 3, 6), new DateOnly(2026, 3, 9)).Value;

        _counter.CountDays(period, holidays: new HashSet<DateOnly>(), sandwichRuleEnabled: true).Should().Be(4m);
    }

    [Fact]
    public void CountDays_Should_Exclude_A_Holiday_Inside_The_Range_When_The_Sandwich_Rule_Is_Off()
    {
        var period = DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 4)).Value;
        var holidays = new HashSet<DateOnly> { new(2026, 3, 3) };

        _counter.CountDays(period, holidays, sandwichRuleEnabled: false).Should().Be(2m);
    }
}
