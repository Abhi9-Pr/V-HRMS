using System.Text.Json;
using FluentAssertions;
using Vespera.Api.Serialization;

namespace Vespera.Api.IntegrationTests;

/// <summary>
/// Pure unit coverage for the converter itself — no WebApplicationFactory needed. See the
/// converter's own doc comment for why this exists: every generated Angular API client sends a
/// full ISO-8601 instant (via JS <c>Date.toJSON()</c>) for any <see cref="DateOnly"/> field, which
/// System.Text.Json's built-in converter rejects outright.
/// </summary>
public class LenientDateOnlyJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new LenientDateOnlyJsonConverter() } };

    [Fact]
    public void Read_Should_Accept_The_Strict_Date_Only_Format()
    {
        var result = JsonSerializer.Deserialize<DateOnly>("\"2026-09-17\"", Options);

        result.Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void Read_Should_Accept_A_Full_Iso_Instant_From_JS_Date_ToJSON()
    {
        var result = JsonSerializer.Deserialize<DateOnly>("\"2026-09-17T00:00:00.000Z\"", Options);

        result.Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void Read_Should_Throw_For_An_Unparseable_Value()
    {
        var act = () => JsonSerializer.Deserialize<DateOnly>("\"not-a-date\"", Options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Write_Should_Emit_The_Strict_Date_Only_Format()
    {
        var json = JsonSerializer.Serialize(new DateOnly(2026, 9, 17), Options);

        json.Should().Be("\"2026-09-17\"");
    }
}
