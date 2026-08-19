using Microsoft.EntityFrameworkCore;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.Persistence.Idempotency;

/// <summary>Persists immediately (calls SaveChangesAsync itself) — there is no MediatR
/// TransactionBehavior at the HTTP-middleware layer to defer to; see IIdempotencyResponseCache.</summary>
public sealed class EfIdempotencyResponseCache : IIdempotencyResponseCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly VesperaDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EfIdempotencyResponseCache(VesperaDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CachedHttpResponse?> GetAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        // The expiry check is applied client-side: EF's Sqlite provider can't translate a
        // DateTimeOffset comparison combined with the key equality in one WHERE (the same
        // limitation worked around elsewhere — see AuditLogRetentionTarget/OutboxDispatcherHostedService).
        var entity = await _dbContext.Set<IdempotencyResponseEntity>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken);

        if (entity is null || entity.ExpiresAt <= _dateTimeProvider.UtcNow)
        {
            return null;
        }

        return new CachedHttpResponse(entity.StatusCode, entity.ContentType, entity.Body);
    }

    public async Task StoreAsync(string idempotencyKey, CachedHttpResponse response, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        _dbContext.Add(new IdempotencyResponseEntity
        {
            IdempotencyKey = idempotencyKey,
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            Body = response.Body,
            CreatedAt = now,
            ExpiresAt = now.Add(Ttl),
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
