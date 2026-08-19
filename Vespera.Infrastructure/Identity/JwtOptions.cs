namespace Vespera.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Vespera:Jwt";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 bytes/UTF-8 chars — validated at
    /// startup by VesperaJwtOptionsValidator. Local dev only; a real deployment supplies this via
    /// user-secrets/environment, never checked into appsettings.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Vespera";

    public string Audience { get; set; } = "Vespera.Client";
}
