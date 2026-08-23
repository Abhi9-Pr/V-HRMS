namespace Vespera.Infrastructure.Persistence.Auditing;

/// <summary>
/// One row per explicit PII reveal/unmask action. A pure persistence model, not a Domain
/// aggregate — same spirit as <see cref="AuditLogEntity"/>, but for reads instead of writes:
/// <c>AuditLogInterceptor</c> only fires on <c>SaveChanges</c>, so an unmask action (which never
/// writes anything) needs its own explicit row, written by
/// <c>Vespera.Infrastructure.Security.PiiAccessAuditor</c>.
/// </summary>
public sealed class PiiAccessAuditEntry
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string SubjectType { get; set; } = string.Empty;

    public Guid SubjectId { get; set; }

    public string Field { get; set; } = string.Empty;

    public string AccessedBy { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}
