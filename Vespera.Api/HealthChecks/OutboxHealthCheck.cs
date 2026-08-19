using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Outbox;

namespace Vespera.Api.HealthChecks;

/// <summary>Degraded if any Pending row has sat around longer than the dispatcher's poll interval
/// would explain — a sign the dispatcher isn't running rather than a hard outage.</summary>
public sealed class OutboxHealthCheck : IHealthCheck
{
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromMinutes(5);

    private readonly VesperaDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutboxHealthCheck(VesperaDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cutoff = _dateTimeProvider.UtcNow.Subtract(StuckThreshold);

        // The cutoff comparison is applied client-side: EF's Sqlite provider can't translate a
        // DateTimeOffset comparison combined with the enum equality in one WHERE (the same
        // limitation worked around elsewhere — see AuditLogRetentionTarget/EfIdempotencyResponseCache).
        // Pending rows are a small, actively-drained set, so this is a fine trade for portability.
        var pending = await _dbContext.Set<OutboxMessageEntity>()
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .Select(m => m.OccurredOn)
            .ToListAsync(cancellationToken);
        var stuckCount = pending.Count(occurredOn => occurredOn < cutoff);

        return stuckCount == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded($"{stuckCount} outbox message(s) pending for longer than {StuckThreshold}.");
    }
}
