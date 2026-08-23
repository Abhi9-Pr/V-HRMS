using Vespera.Domain.Common;

namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// Records a PII *read* (an explicit unmask/reveal action). Distinct from
/// <c>AuditLogInterceptor</c>, which only fires on EF <c>SaveChanges</c> (insert/update/delete) —
/// unmasking never touches the database via a write, so it needs its own explicit call site.
/// </summary>
public interface IPiiAccessAuditor
{
    public Task RecordAccessAsync(
        TenantId tenantId,
        string subjectType,
        Guid subjectId,
        string field,
        string accessedBy,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken);
}
