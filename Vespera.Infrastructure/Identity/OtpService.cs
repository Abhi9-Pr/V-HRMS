using OtpNet;
using Vespera.Application.Abstractions.Identity;

namespace Vespera.Infrastructure.Identity;

/// <summary>RFC 6238 TOTP via Otp.NET — see the Phase 4 plan for why this isn't hand-rolled.</summary>
public sealed class OtpService : IOtpService
{
    private const string Issuer = "Vespera";

    public string GenerateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public string GenerateQrCodeUri(string secret, string accountEmail)
    {
        var label = Uri.EscapeDataString($"{Issuer}:{accountEmail}");
        var issuer = Uri.EscapeDataString(Issuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool ValidateCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }
}
