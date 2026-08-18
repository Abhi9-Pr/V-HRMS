using Vespera.Domain.Common;

namespace Vespera.Domain.IdentityAccess;

public readonly record struct RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
}

public sealed class RefreshToken : AggregateRoot<RefreshTokenId>, ITenantScoped
{
    private RefreshToken(RefreshTokenId id, TenantId tenantId, UserId userId, string tokenHash, DateTimeOffset expiresAt)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public TenantId TenantId { get; }

    public UserId UserId { get; }

    public string TokenHash { get; }

    public DateTimeOffset ExpiresAt { get; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static Result<RefreshToken> Issue(TenantId tenantId, UserId userId, string tokenHash, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Result.Failure<RefreshToken>(Error.Validation("refresh_token.hash_required", "Token hash is required."));
        }

        return Result.Success(new RefreshToken(RefreshTokenId.New(), tenantId, userId, tokenHash, expiresAt));
    }

    public Result Revoke(DateTimeOffset occurredOn)
    {
        if (RevokedAt is not null)
        {
            return Result.Failure(Error.Conflict("refresh_token.already_revoked", "Token is already revoked."));
        }

        RevokedAt = occurredOn;
        return Result.Success();
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}
