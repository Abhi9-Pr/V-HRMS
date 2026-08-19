namespace Vespera.Application.Abstractions.Identity;

/// <summary>TOTP (RFC 6238) enrolment and verification. Kept as a thin port so the concrete
/// algorithm implementation (Infrastructure) is swappable without touching Application.</summary>
public interface IOtpService
{
    public string GenerateSecret();

    /// <summary>An <c>otpauth://</c> URI suitable for rendering as a QR code in an authenticator app.</summary>
    public string GenerateQrCodeUri(string secret, string accountEmail);

    public bool ValidateCode(string secret, string code);
}
