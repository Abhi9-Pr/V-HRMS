using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess.Events;

namespace Vespera.Domain.IdentityAccess;

public readonly record struct RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
}

/// <summary>
/// One refresh token in a rotation chain. Every token issued from the same login (and every
/// token produced by rotating it) shares <see cref="FamilyId"/>. Rotation revokes the presented
/// token and issues a new one in the same family (<see cref="Rotate"/>); presenting a token whose
/// <see cref="ReplacedByTokenId"/> is already set is reuse of an already-rotated-away token —
/// the caller (see <c>RefreshTokenCommandHandler</c>) revokes the whole family and calls
/// <see cref="MarkReuseDetected"/>, which raises a security-event domain event delivered through
/// the standard outbox (see <c>DomainEventDispatchInterceptor</c>).
/// </summary>
public sealed class RefreshToken : AggregateRoot<RefreshTokenId>, ITenantScoped
{
    private RefreshToken(
        RefreshTokenId id, TenantId tenantId, UserId userId, string tokenHash, string deviceId, Guid familyId,
        DateTimeOffset createdAt, DateTimeOffset expiresAt)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        DeviceId = deviceId;
        FamilyId = familyId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public TenantId TenantId { get; }

    public UserId UserId { get; }

    public string TokenHash { get; }

    public string DeviceId { get; }

    /// <summary>Shared by every token in one rotation chain — the unit reuse-detection revokes.</summary>
    public Guid FamilyId { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Set once this token is rotated away. A non-null value on a token being presented
    /// again is the reuse signal.</summary>
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public static Result<RefreshToken> Issue(
        TenantId tenantId, UserId userId, string tokenHash, string deviceId, DateTimeOffset occurredOn, DateTimeOffset expiresAt,
        Guid? familyId = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Result.Failure<RefreshToken>(Error.Validation("refresh_token.hash_required", "Token hash is required."));
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return Result.Failure<RefreshToken>(Error.Validation("refresh_token.device_id_required", "Device id is required."));
        }

        return Result.Success(new RefreshToken(
            RefreshTokenId.New(), tenantId, userId, tokenHash, deviceId.Trim(), familyId ?? Guid.NewGuid(), occurredOn, expiresAt));
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

    public Result<RefreshToken> Rotate(string newTokenHash, DateTimeOffset occurredOn, DateTimeOffset newExpiresAt)
    {
        if (!IsActive(occurredOn))
        {
            return Result.Failure<RefreshToken>(Error.Conflict("refresh_token.not_active", "Token is not active and cannot be rotated."));
        }

        var next = new RefreshToken(RefreshTokenId.New(), TenantId, UserId, newTokenHash, DeviceId, FamilyId, occurredOn, newExpiresAt);
        RevokedAt = occurredOn;
        ReplacedByTokenId = next.Id;
        return Result.Success(next);
    }

    /// <summary>Records that this already-rotated-away token was presented again — a credential
    /// theft signal. Does not itself revoke the family; the caller revokes every active token
    /// sharing <see cref="FamilyId"/> and calls this on the replayed token to raise the event.</summary>
    public void MarkReuseDetected(DateTimeOffset occurredOn) =>
        Raise(new RefreshTokenReuseDetected(UserId, TenantId, DeviceId, FamilyId, occurredOn));
}
