using Vespera.Domain.Common;

namespace Vespera.Domain.IdentityAccess.Events;

/// <summary>Raised when an already-rotated-away refresh token is presented again — the entire
/// token family has been revoked in response. A likely credential-theft signal.</summary>
public sealed record RefreshTokenReuseDetected(UserId UserId, TenantId TenantId, string DeviceId, Guid FamilyId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
