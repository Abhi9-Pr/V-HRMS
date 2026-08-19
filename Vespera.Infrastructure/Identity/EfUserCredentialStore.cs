using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.Identity;

/// <summary>
/// Implements <see cref="IUserCredentialStore"/> using <see cref="UserManager{TUser}"/> for
/// password hashing/verification/reset tokens, and direct <see cref="VesperaDbContext"/> access +
/// <see cref="IOtpService"/> for TOTP — see <see cref="VesperaUserStore"/>'s doc comment for why
/// two-factor doesn't go through <c>UserManager</c>.
/// </summary>
public sealed class EfUserCredentialStore : IUserCredentialStore
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpService _otpService;
    private readonly VesperaDbContext _dbContext;

    public EfUserCredentialStore(UserManager<ApplicationUser> userManager, IOtpService otpService, VesperaDbContext dbContext)
    {
        _userManager = userManager;
        _otpService = otpService;
        _dbContext = dbContext;
    }

    public async Task<Result> CreateAsync(UserId userId, string email, string password, CancellationToken cancellationToken)
    {
        var applicationUser = new ApplicationUser { Id = userId.Value, UserName = email, Email = email };
        var result = await _userManager.CreateAsync(applicationUser, password);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(Error.Validation("auth.invalid_password", DescribeErrors(result)));
    }

    public async Task<bool> VerifyPasswordAsync(UserId userId, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        return user is not null && await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<Result> ChangePasswordAsync(UserId userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("auth.user_not_found", "User not found."));
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded ? Result.Success() : Result.Failure(Error.Validation("auth.invalid_password", DescribeErrors(result)));
    }

    public async Task<string> GeneratePasswordResetTokenAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        return user is null ? string.Empty : await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<Result> ResetPasswordAsync(UserId userId, string resetToken, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result.Failure(InvalidResetToken);
        }

        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        return result.Succeeded ? Result.Success() : Result.Failure(InvalidResetToken);
    }

    public async Task<string> BeginTwoFactorEnrollmentAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"No credential record for user '{userId.Value}'.");

        var secret = _otpService.GenerateSecret();
        user.AuthenticatorKey = secret;
        return secret;
    }

    public async Task<Result> ConfirmTwoFactorEnrollmentAsync(UserId userId, string code, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user?.AuthenticatorKey is null)
        {
            return Result.Failure(Error.Conflict("auth.totp_not_enrolled", "Start enrolment before confirming a code."));
        }

        if (!_otpService.ValidateCode(user.AuthenticatorKey, code))
        {
            return Result.Failure(Error.Validation("auth.totp_invalid_code", "The authenticator code is not valid."));
        }

        user.TwoFactorEnabled = true;
        return Result.Success();
    }

    public async Task<bool> IsTwoFactorEnabledAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<ApplicationUser>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        return user?.TwoFactorEnabled ?? false;
    }

    public async Task<bool> ValidateTwoFactorCodeAsync(UserId userId, string code, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<ApplicationUser>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        return user?.AuthenticatorKey is not null && user.TwoFactorEnabled && _otpService.ValidateCode(user.AuthenticatorKey, code);
    }

    private static readonly Error InvalidResetToken = Error.Unauthorized("auth.invalid_reset_token", "This reset token is not valid.");

    private static string DescribeErrors(IdentityResult result) => string.Join("; ", result.Errors.Select(e => e.Description));
}
