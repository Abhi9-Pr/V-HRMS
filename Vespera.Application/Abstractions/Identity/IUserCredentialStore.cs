using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Abstractions.Identity;

/// <summary>
/// Credential lifecycle for one user — password hash, TOTP secret/enrolment. Deliberately
/// separate from <see cref="User"/> (Domain), which models identity/roles/status but never
/// credentials (see the comment on <see cref="User"/>). Backed by ASP.NET Core Identity's
/// <c>UserManager</c> in Infrastructure; Application only ever sees this narrow port.
/// </summary>
public interface IUserCredentialStore
{
    public Task<Result> CreateAsync(UserId userId, string email, string password, CancellationToken cancellationToken);

    public Task<bool> VerifyPasswordAsync(UserId userId, string password, CancellationToken cancellationToken);

    public Task<Result> ChangePasswordAsync(UserId userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    public Task<string> GeneratePasswordResetTokenAsync(UserId userId, CancellationToken cancellationToken);

    public Task<Result> ResetPasswordAsync(UserId userId, string resetToken, string newPassword, CancellationToken cancellationToken);

    /// <summary>Generates and stores a new (not-yet-confirmed) TOTP secret, returning it for the
    /// caller to render as a QR code via <see cref="IOtpService"/>. Two-factor stays disabled
    /// until <see cref="ConfirmTwoFactorEnrollmentAsync"/> succeeds.</summary>
    public Task<string> BeginTwoFactorEnrollmentAsync(UserId userId, CancellationToken cancellationToken);

    public Task<Result> ConfirmTwoFactorEnrollmentAsync(UserId userId, string code, CancellationToken cancellationToken);

    public Task<bool> IsTwoFactorEnabledAsync(UserId userId, CancellationToken cancellationToken);

    public Task<bool> ValidateTwoFactorCodeAsync(UserId userId, string code, CancellationToken cancellationToken);
}
