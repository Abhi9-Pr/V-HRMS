namespace Vespera.Infrastructure.Identity;

/// <summary>
/// Credentials only — email/password/TOTP/recovery codes. <see cref="Id"/> equals the Domain
/// <c>User.Id.Value</c> (no FK — same cross-aggregate-by-id convention used throughout this
/// codebase). Identity/roles/status live on the Domain <c>User</c>; this table exists purely so
/// <c>VesperaUserStore</c> can back <see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}"/>.
/// Deliberately has no lockout fields — see the Phase 4 plan for why account lockout stays a
/// Domain concept (<c>User.Status</c>) instead of a second, competing source of truth.
/// </summary>
public sealed class ApplicationUser
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string NormalizedUserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string SecurityStamp { get; set; } = string.Empty;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public bool TwoFactorEnabled { get; set; }

    /// <summary>The TOTP shared secret, set (but not yet trusted — see TwoFactorEnabled) as soon
    /// as enrolment begins.</summary>
    public string? AuthenticatorKey { get; set; }

    /// <summary>Semicolon-joined, one-time recovery codes — mirrors ASP.NET Core Identity's own
    /// default token-provider storage shape, just persisted as one column instead of a separate
    /// token table.</summary>
    public string? RecoveryCodesConcatenated { get; set; }
}
