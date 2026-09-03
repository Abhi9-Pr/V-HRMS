using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vespera.Api.Serialization;

/// <summary>
/// System.Text.Json's built-in <see cref="DateOnly"/> converter only accepts a strict
/// "yyyy-MM-dd" string. Angular's Material datepicker binds a JS <c>Date</c>, and
/// <c>JSON.stringify</c> serializes that via <c>Date.toJSON()</c> as a full ISO-8601 instant
/// (e.g. "2026-09-17T00:00:00.000Z") — every generated API client method that takes a
/// <see cref="DateOnly"/> parameter sends exactly that shape, so the strict converter rejects it
/// with a 400 on every such request. Falls back to parsing the date component of a full
/// date-time string when the strict "yyyy-MM-dd" parse fails; writes are unchanged (still plain
/// "yyyy-MM-dd"), so this only widens what's accepted, not what the API itself emits.
/// </summary>
public sealed class LenientDateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("Expected a non-empty date string.");
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var strict))
        {
            return strict;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffset))
        {
            return DateOnly.FromDateTime(dateTimeOffset.UtcDateTime);
        }

        throw new JsonException($"Could not parse '{value}' as a date.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
