namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// Deactivates a user's login and revokes every active session — what an exited employee's
/// last-working-day sweep calls (see <c>OffboardingAccessRevocationHostedService</c>). Kept as a
/// narrow port so the sweep never has to know how "revoke access" is actually implemented
/// (disabling a row, revoking tokens, or eventually calling out to an external IdP).
/// </summary>
public interface IAccessRevocationService
{
    public Task RevokeAccessAsync(Guid userId, CancellationToken cancellationToken);
}
