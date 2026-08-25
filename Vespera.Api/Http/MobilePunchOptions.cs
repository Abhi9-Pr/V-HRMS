namespace Vespera.Api.Http;

/// <summary>Thresholds for the mobile punch endpoint's soft-signal checks. Resolved here (not in
/// Application — see <c>RecordMobilePunchCommand</c>'s doc comment) and threaded into the command,
/// the same config-at-the-edge pattern <see cref="WebPunchOptions"/> already uses.</summary>
public sealed class MobilePunchOptions
{
    public const string SectionName = "Vespera:MobilePunch";

    /// <summary>Default true: a mock/simulated location provider is almost always a spoofing
    /// attempt in an attendance context. A tenant with a legitimate reason to allow it (e.g.
    /// controlled device testing) can flip this to flag-instead-of-reject.</summary>
    public bool RejectMockProvider { get; set; } = true;

    /// <summary>Fast enough to not false-positive a short domestic flight between two work sites;
    /// slow enough to catch GPS-teleportation spoofing.</summary>
    public double MaxPlausibleSpeedKmh { get; set; } = 250;

    public double MinAcceptableAccuracyMetres { get; set; } = 100;
}
