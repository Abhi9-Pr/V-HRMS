using NodaTime;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Services;

/// <summary>
/// <see cref="ITimeZoneConverter"/> backed by NodaTime's tzdb (IANA) provider.
/// <para>
/// <see cref="ToUtc"/> resolves ambiguous/skipped local times leniently
/// (<see cref="DateTimeZone.AtLeniently"/>) rather than strictly: a local time that falls in a
/// DST "spring forward" gap is shifted forward by the gap's length, and a local time that falls
/// in a "fall back" fold picks the earlier of the two valid offsets. A shift boundary or roster
/// anchor landing on a DST transition is an edge case worth tolerating predictably, not a reason
/// to hard-fail an attendance computation — <see cref="DateTimeZone.AtStrictly"/> would throw for
/// both cases instead.
/// </para>
/// </summary>
public sealed class NodaTimeZoneConverter : ITimeZoneConverter
{
    public DateTimeOffset ToUtc(LocalDateTime local, string timeZoneId)
    {
        var zone = ResolveZone(timeZoneId);
        var zoned = zone.AtLeniently(local);
        return zoned.ToDateTimeOffset();
    }

    public ZonedDateTime ToZoned(DateTimeOffset utc, string timeZoneId)
    {
        var zone = ResolveZone(timeZoneId);
        var instant = Instant.FromDateTimeOffset(utc);
        return instant.InZone(zone);
    }

    public DateOnly ResolveLocalDate(DateTimeOffset utc, string timeZoneId)
    {
        var zoned = ToZoned(utc, timeZoneId);
        var date = zoned.Date;
        return new DateOnly(date.Year, date.Month, date.Day);
    }

    private static DateTimeZone ResolveZone(string timeZoneId)
    {
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId);
        if (zone is null)
        {
            // An unknown IANA id is a configuration/programmer error (a Location's TimeZoneId was
            // set to something invalid) — per AGENTS.md, exceptions are for exactly that, not an
            // expected Result failure a caller should be branching on.
            throw new InvalidOperationException($"Unknown IANA time zone id: '{timeZoneId}'.");
        }

        return zone;
    }
}
