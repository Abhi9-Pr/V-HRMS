using Microsoft.EntityFrameworkCore;
using Vespera.Domain.Compliance;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Auditing;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>Enforces the "AuditLog" retention category by hard-deleting rows past the window.
/// Audit rows are already PII-redacted at write time (see AuditLogInterceptor), so there's no
/// meaningful "Anonymize" distinct from "Purge" for this category — both act the same way here.</summary>
public sealed class AuditLogRetentionTarget : IRetentionTarget
{
    public string Category => "AuditLog";

    public async Task ApplyAsync(Guid tenantId, DateTimeOffset cutoff, RetentionAction action, VesperaDbContext dbContext, CancellationToken cancellationToken)
    {
        // The Timestamp cutoff check is applied client-side: EF's Sqlite provider (the dev
        // fallback at runtime, and used in unit tests) can't translate a DateTimeOffset
        // comparison combined with the TenantId equality in one WHERE. A tenant's audit log for
        // one retention pass is a small, bounded set, so this is a fine trade for portability.
        var stale = (await dbContext.Set<AuditLogEntity>()
                .Where(log => log.TenantId == tenantId)
                .ToListAsync(cancellationToken))
            .Where(log => log.Timestamp < cutoff);

        dbContext.RemoveRange(stale);
    }
}
