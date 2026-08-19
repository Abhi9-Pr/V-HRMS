namespace Vespera.Infrastructure.Persistence.Idempotency;

/// <summary>Backing row for <see cref="IdempotencyStore"/> — durable state for <c>IdempotencyBehavior</c>.</summary>
public sealed class IdempotencyRecordEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}
