using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Auditing;

namespace Vespera.Infrastructure.Security;

/// <summary>
/// Stages one <see cref="PiiAccessAuditEntry"/> row per reveal action. Like every other write in
/// this codebase, <c>TransactionBehavior</c> is what actually calls <c>SaveChangesAsync</c> — see
/// <c>EfOutboxWriter</c> for the same stage-only pattern.
/// </summary>
public sealed class PiiAccessAuditor : IPiiAccessAuditor
{
    private readonly VesperaDbContext _dbContext;

    public PiiAccessAuditor(VesperaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAccessAsync(
        TenantId tenantId,
        string subjectType,
        Guid subjectId,
        string field,
        string accessedBy,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<PiiAccessAuditEntry>().AddAsync(
            new PiiAccessAuditEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                SubjectType = subjectType,
                SubjectId = subjectId,
                Field = field,
                AccessedBy = accessedBy,
                OccurredAt = occurredOn,
            },
            cancellationToken);
}
