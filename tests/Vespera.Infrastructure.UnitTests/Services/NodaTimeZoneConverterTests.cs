using FluentAssertions;
using NodaTime;
using Vespera.Infrastructure.Services;

namespace Vespera.Infrastructure.UnitTests.Services;

public class NodaTimeZoneConverterTests
{
    private readonly NodaTimeZoneConverter _converter = new();

    [Fact]
    public void ToUtc_And_ToZoned_Should_RoundTrip_For_A_Non_Dst_Zone()
    {
        var local = new LocalDateTime(2026, 6, 15, 10, 30, 0);

        var utc = _converter.ToUtc(local, "Asia/Kolkata");

        // Asia/Kolkata is a fixed UTC+5:30 offset year-round — no DST ambiguity to resolve.
        utc.Offset.Should().Be(TimeSpan.FromHours(5.5));

        var zoned = _converter.ToZoned(utc, "Asia/Kolkata");
        zoned.Year.Should().Be(2026);
        zoned.Month.Should().Be(6);
        zoned.Day.Should().Be(15);
        zoned.Hour.Should().Be(10);
        zoned.Minute.Should().Be(30);
    }

    [Fact]
    public void ToUtc_Should_Resolve_A_Spring_Forward_Gap_Leniently_Instead_Of_Throwing()
    {
        // America/New_York, 2024-03-10: clocks jump 02:00 -> 03:00. 02:30 never occurs locally.
        var localInGap = new LocalDateTime(2024, 3, 10, 2, 30, 0);

        var act = () => _converter.ToUtc(localInGap, "America/New_York");

        act.Should().NotThrow();

        var utc = _converter.ToUtc(localInGap, "America/New_York");

        // A time inside the gap can only leniently resolve to somewhere in the post-transition
        // (EDT, UTC-4) period — there is no valid EST interpretation of "02:30" that day.
        utc.Offset.Should().Be(TimeSpan.FromHours(-4));
    }

    [Fact]
    public void ToUtc_Should_Resolve_A_Fall_Back_Fold_Leniently_Instead_Of_Throwing()
    {
        // America/New_York, 2024-11-03: clocks fall back 02:00 -> 01:00. 01:30 occurs twice —
        // once at UTC-4 (EDT, before the transition) and once at UTC-5 (EST, after).
        var localInFold = new LocalDateTime(2024, 11, 3, 1, 30, 0);

        var act = () => _converter.ToUtc(localInFold, "America/New_York");

        act.Should().NotThrow();

        var utc = _converter.ToUtc(localInFold, "America/New_York");

        // Either valid occurrence is an acceptable lenient resolution — the requirement is a
        // predictable, non-throwing result, not a specific tie-break policy.
        var isEitherValidOffset = utc.Offset == TimeSpan.FromHours(-4) || utc.Offset == TimeSpan.FromHours(-5);
        isEitherValidOffset.Should().BeTrue($"expected -4h or -5h but got {utc.Offset}");
    }

    [Fact]
    public void ResolveLocalDate_Should_Identify_The_Local_Calendar_Day_Not_The_Utc_One()
    {
        // 2026-01-01 20:00 UTC is already 2026-01-02 01:30 in Asia/Kolkata (UTC+5:30).
        var utc = new DateTimeOffset(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

        var localDate = _converter.ResolveLocalDate(utc, "Asia/Kolkata");

        localDate.Should().Be(new DateOnly(2026, 1, 2));
    }

    [Fact]
    public void ResolveLocalDate_Should_Match_Utc_Date_When_The_Zone_Is_Utc()
    {
        var utc = new DateTimeOffset(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

        var localDate = _converter.ResolveLocalDate(utc, "Etc/UTC");

        localDate.Should().Be(new DateOnly(2026, 1, 1));
    }
}
