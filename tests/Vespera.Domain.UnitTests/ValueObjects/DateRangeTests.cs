using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Create_With_End_Before_Start_Should_Fail()
    {
        var result = DateRange.Create(new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_Should_Be_True_For_Intersecting_Ranges()
    {
        var first = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10)).Value;
        var second = DateRange.Create(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 15)).Value;

        first.Overlaps(second).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_Should_Be_False_For_Disjoint_Ranges()
    {
        var first = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10)).Value;
        var second = DateRange.Create(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 10)).Value;

        first.Overlaps(second).Should().BeFalse();
    }

    [Fact]
    public void TotalDays_Should_Be_Inclusive_Of_Both_Endpoints()
    {
        var range = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5)).Value;

        range.TotalDays.Should().Be(5);
    }

    [Fact]
    public void Contains_Should_Be_True_For_A_Date_Inside_The_Range()
    {
        var range = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10)).Value;

        range.Contains(new DateOnly(2026, 1, 5)).Should().BeTrue();
        range.Contains(new DateOnly(2026, 2, 1)).Should().BeFalse();
    }

    [Fact]
    public void Instances_With_The_Same_Bounds_Should_Be_Equal()
    {
        var first = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10)).Value;
        var second = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10)).Value;
        var different = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15)).Value;

        first.Should().Be(second);
        first.Should().NotBe(different);
    }
}
