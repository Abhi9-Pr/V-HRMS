using System.Globalization;

namespace Vespera.Application.Features.Recruitment;

/// <summary>
/// Hand-generates a minimal RFC 5545 VCALENDAR/VEVENT block as text. This codebase's
/// <c>NotificationMessage</c>/<c>INotificationChannel</c> has no attachment concept, so the
/// "calendar invite" is delivered as plain text in a notification's <c>Metadata</c>, not a real
/// <c>.ics</c> MIME attachment — a real implementation would attach one via an actual email
/// provider. This is an honest, dependency-free seam, not a full calendaring integration.
/// </summary>
public static class IcsInviteGenerator
{
    public static string Generate(string summary, DateTimeOffset start, TimeSpan duration, string organizerEmail, IReadOnlyList<string> attendeeEmails)
    {
        var end = start.Add(duration);
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Vespera HRMS//Recruitment//EN",
            "BEGIN:VEVENT",
            $"UID:{Guid.NewGuid()}",
            $"DTSTAMP:{FormatUtc(DateTimeOffset.UtcNow)}",
            $"DTSTART:{FormatUtc(start)}",
            $"DTEND:{FormatUtc(end)}",
            $"SUMMARY:{Escape(summary)}",
            $"ORGANIZER:mailto:{organizerEmail}",
        };

        lines.AddRange(attendeeEmails.Select(email => $"ATTENDEE:mailto:{email}"));
        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");

        return string.Join("\r\n", lines);
    }

    private static string FormatUtc(DateTimeOffset value) => value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string value) => value.Replace(",", "\\,", StringComparison.Ordinal).Replace(";", "\\;", StringComparison.Ordinal);
}
