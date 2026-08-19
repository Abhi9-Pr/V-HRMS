using Vespera.Domain.Compliance;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// Enforces one <see cref="RetentionPolicy.EntityCategory"/> — the OCP seam for DPDP retention: a
/// future slice that introduces a new retained data category registers one more implementation
/// (DI registration only), <see cref="RetentionPurgeHostedService"/> never changes. Only
/// categories with a registered target actually get enforced; a seeded policy for a category with
/// no registered target is configuration-only until that slice exists (see docs/data-protection.md).
/// </summary>
public interface IRetentionTarget
{
    public string Category { get; }

    public Task ApplyAsync(Guid tenantId, DateTimeOffset cutoff, RetentionAction action, VesperaDbContext dbContext, CancellationToken cancellationToken);
}
