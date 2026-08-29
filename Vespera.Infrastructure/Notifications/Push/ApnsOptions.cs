namespace Vespera.Infrastructure.Notifications.Push;

/// <summary>Apple Push Notification service (iOS) credentials — a p8 signing key (<see
/// cref="KeyId"/>/<see cref="PrivateKeyPem"/>), the Apple developer team id, and the app's bundle
/// id (sent as <c>apns-topic</c>). See <see cref="ApnsPushSender"/>'s doc comment for how these
/// back its per-request JWT.</summary>
public sealed class ApnsOptions
{
    public const string SectionName = "Vespera:Apns";

    public string KeyId { get; set; } = string.Empty;

    public string TeamId { get; set; } = string.Empty;

    public string BundleId { get; set; } = string.Empty;

    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>True for Apple's sandbox APNs host (development-signed builds); false for
    /// production. See <c>MobilePushOptions</c>-style config-at-the-edge precedent — this is a
    /// per-environment deployment setting, not something a caller decides per message.</summary>
    public bool UseSandbox { get; set; } = true;
}
