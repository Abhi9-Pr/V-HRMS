namespace Vespera.Infrastructure.Persistence.Auditing;

public enum AuditActionType
{
    Created,
    Modified,
    Deleted,
}

/// <summary>
/// One audit trail row, written by <see cref="Interceptors.AuditLogInterceptor"/> for every
/// tracked entity change. A pure persistence model, not a Domain aggregate — auditing is an
/// infrastructure concern with no business behavior of its own.
/// </summary>
public sealed class AuditLogEntity
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public AuditActionType ActionType { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string EntityKey { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? OldValueJson { get; set; }

    public string? NewValueJson { get; set; }
}
