namespace Vespera.Api.Http;

/// <summary>Per-platform minimum supported app version — below this, <see
/// cref="Middleware.MinAppVersionMiddleware"/> rejects the request with a force-upgrade response
/// instead of letting an app version the API may have stopped supporting call in. Same
/// config-at-the-edge pattern as <see cref="WebPunchOptions"/>/<see cref="MobilePunchOptions"/>.</summary>
public sealed class MobileMinVersionOptions
{
    public const string SectionName = "Vespera:MobileMinVersion";

    public string Ios { get; set; } = "1.0.0";

    public string Android { get; set; } = "1.0.0";
}
