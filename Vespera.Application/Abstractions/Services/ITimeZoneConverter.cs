using NodaTime;

namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// Converts between UTC (how everything is stored, per AGENTS.md) and a location's local time
/// (how attendance/shift/roster computations must reason about "day," "late," "overtime," etc.).
/// Backed by NodaTime rather than the BCL's <see cref="TimeZoneInfo"/> so ambiguous/skipped local
/// times around a DST transition resolve predictably instead of needing hand-rolled
/// IsAmbiguousTime/IsInvalidTime handling at every call site.
/// </summary>
public interface ITimeZoneConverter
{
    /// <summary>Resolves <paramref name="local"/> in <paramref name="timeZoneId"/> to a UTC
    /// instant. A local time that falls in a DST gap or fold is resolved leniently (shifted into
    /// the nearest valid instant) rather than throwing — see the Infrastructure implementation's
    /// doc comment for why.</summary>
    public DateTimeOffset ToUtc(LocalDateTime local, string timeZoneId);

    /// <summary>Resolves a UTC instant to its <see cref="ZonedDateTime"/> in
    /// <paramref name="timeZoneId"/>.</summary>
    public ZonedDateTime ToZoned(DateTimeOffset utc, string timeZoneId);

    /// <summary>The local calendar date <paramref name="utc"/> falls on in
    /// <paramref name="timeZoneId"/> — e.g. an instant that's already "tomorrow" in a
    /// UTC+offset zone but still "today" in UTC.</summary>
    public DateOnly ResolveLocalDate(DateTimeOffset utc, string timeZoneId);
}
