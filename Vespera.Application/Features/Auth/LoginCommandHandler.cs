using System.Security.Cryptography;
using System.Text;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Authorization;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Auth;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly IReadRepository<User> _users;
    private readonly IWriteRepository<RefreshToken> _refreshTokens;
    private readonly IUserCredentialStore _credentialStore;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PermissionResolver _permissionResolver;

    public LoginCommandHandler(
        IReadRepository<User> users, IWriteRepository<RefreshToken> refreshTokens, IUserCredentialStore credentialStore,
        ITokenService tokenService, ITenantContext tenantContext, IDateTimeProvider dateTimeProvider, PermissionResolver permissionResolver)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _credentialStore = credentialStore;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _permissionResolver = permissionResolver;
    }

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<LoginResult>(InvalidCredentials);
        }

        var user = await _users.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (user is null || !await _credentialStore.VerifyPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            return Result.Failure<LoginResult>(InvalidCredentials);
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<LoginResult>(Error.Forbidden("auth.account_not_active", "This account is locked or deactivated."));
        }

        var claims = await _permissionResolver.ResolveAsync(user.RoleIds, cancellationToken);

        if (claims.PermissionCodes.Contains(Permissions.Finance.Admin))
        {
            if (!await _credentialStore.IsTwoFactorEnabledAsync(user.Id, cancellationToken))
            {
                return Result.Failure<LoginResult>(Error.Forbidden(
                    "auth.totp_enrollment_required", "Two-factor authentication must be enrolled before this account can sign in."));
            }

            if (string.IsNullOrWhiteSpace(request.TotpCode) ||
                !await _credentialStore.ValidateTwoFactorCodeAsync(user.Id, request.TotpCode, cancellationToken))
            {
                return Result.Failure<LoginResult>(Error.Unauthorized("auth.totp_required", "A valid authenticator code is required."));
            }
        }

        var now = _dateTimeProvider.UtcNow;

        var accessToken = _tokenService.GenerateAccessToken(new TokenClaims(
            user.Id.Value, _tenantContext.TenantId, user.Email.Value, claims.RoleNames, claims.PermissionCodes));

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Issue(
            _tenantContext.TenantId, user.Id, Hash(rawRefreshToken), request.DeviceId, now, now.Add(AuthTokenLifetimes.RefreshToken));

        if (refreshToken.IsFailure)
        {
            return Result.Failure<LoginResult>(refreshToken.Error);
        }

        await _refreshTokens.AddAsync(refreshToken.Value, cancellationToken);

        return Result.Success(new LoginResult(accessToken, rawRefreshToken, now.Add(AuthTokenLifetimes.AccessToken)));
    }

    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static readonly Error InvalidCredentials = Error.Unauthorized("auth.invalid_credentials", "Invalid email or password.");
}
