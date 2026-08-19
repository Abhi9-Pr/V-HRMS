namespace Vespera.Infrastructure.Persistence.Outbox;

public enum OutboxMessageStatus
{
    Pending,
    Processed,
    DeadLettered,
}

/// <summary>
/// One transactional-outbox row. Written either automatically by
/// <see cref="Interceptors.DomainEventDispatchInterceptor"/> (one row per raised
/// <c>DomainEvent</c>, in the same <c>SaveChangesAsync</c> call as the entity change that raised
/// it) or explicitly via <see cref="EfOutboxWriter"/> for messages that aren't tied to a domain
/// event. <see cref="BackgroundJobs.OutboxDispatcherHostedService"/> is the only reader.
/// </summary>
public sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    /// <summary>Assembly-qualified CLR type name of the payload, used to deserialize <see cref="Payload"/>.</summary>
    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredOn { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Null means "eligible now". Set on failure to implement backoff between retries.</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }

    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
}
