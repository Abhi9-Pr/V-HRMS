using OtpNet;

namespace Vespera.Api.IntegrationTests;

/// <summary>Computes a valid TOTP code for DevelopmentSeeder's fixed demo Finance.Admin secret,
/// so tests can log in as a Finance.Admin user deterministically.</summary>
internal static class TotpTestHelper
{
    public static string ComputeCurrentCode(string base32Secret) => new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp();
}
