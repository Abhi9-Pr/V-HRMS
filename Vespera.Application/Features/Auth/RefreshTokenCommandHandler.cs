using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

/// <summary>
/// Rotation + reuse detection (see RefreshToken.Rotate/MarkReuseDetected). A presented token that
/// is already revoked because it was rotated away (ReplacedByTokenId set) means someone is
/// replaying a stolen/leaked token — the entire family is revoked and a
/// RefreshTokenReuseDetected security event is raised, delivered through the existing outbox.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResult>>
{
    private static readonly Error InvalidToken = Error.Unauthorized("auth.invalid_refresh_token", "This refresh token is not valid.");

    private readonly IReadRepository<RefreshToken> _readRefreshTokens;
    private readonly IWriteRepository<RefreshToken> _writeRefreshTokens;
    private readonly IReadRepository<User> _users;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PermissionResolver _permissionResolver;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IReadRepository<RefreshToken> readRefreshTokens, IWriteRepository<RefreshToken> writeRefreshTokens, IReadRepository<User> users,
        ITokenService tokenService, ITenantContext tenantContext, IDateTimeProvider dateTimeProvider, PermissionResolver permissionResolver,
        IUnitOfWork unitOfWork)
    {
        _readRefreshTokens = readRefreshTokens;
        _writeRefreshTokens = writeRefreshTokens;
        _users = users;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _permissionResolver = permissionResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var presentedHash = LoginCommandHandler.Hash(request.RefreshToken);

        var presented = await _readRefreshTokens.FirstOrDefaultAsync(new RefreshTokenByHashSpecification(presentedHash), cancellationToken);
        if (presented is null)
        {
            return Result.Failure<LoginResult>(InvalidToken);
        }

        if (presented.ReplacedByTokenId is not null)
        {
            var family = await _readRefreshTokens.ListAsync(
                new ActiveRefreshTokensByFamilySpecification(presented.FamilyId), cancellationToken);

            foreach (var sibling in family)
            {
                sibling.Revoke(now);
            }

            presented.MarkReuseDetected(now);

            // TransactionBehavior only calls SaveChangesAsync when the pipeline returns a
            // successful Result — correct for every ordinary write, but wrong here: revoking a
            // compromised token family is a security action that must be durable precisely
            // *because* this request is being rejected, not despite it. This is the one
            // deliberate exception to "handlers stage, TransactionBehavior saves" in the whole
            // codebase — see CONTRIBUTING-slices.md's non-negotiable #4 for the general rule.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<LoginResult>(Error.Unauthorized(
                "auth.refresh_token_reused", "This refresh token has already been used. All sessions for this device family have been revoked."));
        }

        if (!presented.IsActive(now))
        {
            return Result.Failure<LoginResult>(InvalidToken);
        }

        var user = await _users.FirstOrDefaultAsync(new UserByIdSpecification(presented.UserId), cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return Result.Failure<LoginResult>(InvalidToken);
        }

        var rawNextToken = _tokenService.GenerateRefreshToken();
        var rotated = presented.Rotate(LoginCommandHandler.Hash(rawNextToken), now, now.Add(AuthTokenLifetimes.RefreshToken));
        if (rotated.IsFailure)
        {
            return Result.Failure<LoginResult>(rotated.Error);
        }

        await _writeRefreshTokens.AddAsync(rotated.Value, cancellationToken);

        var claims = await _permissionResolver.ResolveAsync(user.RoleIds, cancellationToken);
        var accessToken = _tokenService.GenerateAccessToken(new TokenClaims(
            user.Id.Value, _tenantContext.TenantId, user.Email.Value, claims.RoleNames, claims.PermissionCodes));

        return Result.Success(new LoginResult(accessToken, rawNextToken, now.Add(AuthTokenLifetimes.AccessToken)));
    }
}
