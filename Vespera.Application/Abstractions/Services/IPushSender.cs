using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Abstractions.Services;

/// <summary><paramref name="Platform"/> is what a composite <see cref="IPushSender"/> routes on
/// (FCM for Android/Web, APNs for iOS) — see docs/api-mobile-contract.md and
/// <c>DeviceRegistration.Platform</c>, which is where a caller reads this from.
/// <paramref name="Data"/> is the deep-link payload (e.g. <c>entityType</c>/<c>entityId</c>/
/// <c>deepLink</c> — see <see cref="NotificationMessage.Metadata"/>, which is where a sender's
/// caller reads these from) delivered alongside the human-readable title/body, so tapping the
/// notification can route straight to the relevant screen instead of just opening the app.</summary>
public sealed record PushMessage(
    string DeviceToken, DevicePlatform Platform, string Title, string Body, IReadOnlyDictionary<string, string> Data);

public interface IPushSender
{
    public Task SendAsync(PushMessage message, CancellationToken cancellationToken);
}
