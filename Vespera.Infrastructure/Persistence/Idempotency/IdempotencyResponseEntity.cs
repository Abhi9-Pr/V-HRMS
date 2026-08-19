namespace Vespera.Infrastructure.Persistence.Idempotency;

/// <summary>Backs the HTTP-level idempotency-replay middleware — see
/// <c>IIdempotencyResponseCache</c> for why this is separate from <see cref="IdempotencyRecordEntity"/>.</summary>
public sealed class IdempotencyResponseEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public int StatusCode { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
