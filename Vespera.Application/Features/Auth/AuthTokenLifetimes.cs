namespace Vespera.Application.Features.Auth;

/// <summary>The exact lifetimes the Phase 4 brief specifies — access tokens 15 minutes, refresh
/// tokens 7 days. Shared by the Auth handlers (which stamp <c>RefreshToken.ExpiresAt</c>) and
/// Infrastructure's <c>JwtTokenService</c> (which stamps the JWT's own <c>exp</c> claim) so the
/// two never drift apart.</summary>
public static class AuthTokenLifetimes
{
    public static readonly TimeSpan AccessToken = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan RefreshToken = TimeSpan.FromDays(7);
}
