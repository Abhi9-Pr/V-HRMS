namespace Vespera.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "Vespera:Storage:Local";

    /// <summary>
    /// HMAC-SHA256 key used to sign download URLs. A clearly-fake placeholder ships in
    /// appsettings for local dev only — production must override this via user-secrets/env,
    /// never commit a real value here.
    /// </summary>
    public string SigningSecret { get; set; } = "local-dev-only-not-a-real-secret";
}
