namespace Vespera.Infrastructure.Identity;

/// <summary>Custom claim type names — the standard <c>System.Security.Claims.ClaimTypes</c>
/// constants cover user id/email/role already (see JwtTokenService/HttpCurrentUser).</summary>
public static class JwtClaimTypes
{
    public const string TenantId = "tenant";

    public const string Permission = "permission";
}
