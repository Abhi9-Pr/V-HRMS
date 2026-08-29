namespace Vespera.Infrastructure.Notifications.Push;

/// <summary>Firebase Cloud Messaging (Android + Web push) credentials — a service account's
/// project id, client email, and PEM private key, the three fields <see cref="FcmPushSender"/>
/// needs to mint its own OAuth2 access tokens (see its doc comment). Deliberately not the whole
/// service-account JSON blob as one string: binding three flat fields from configuration/secrets
/// is simpler than parsing JSON out of an options object.</summary>
public sealed class FcmOptions
{
    public const string SectionName = "Vespera:Fcm";

    public string ProjectId { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public string PrivateKeyPem { get; set; } = string.Empty;
}
