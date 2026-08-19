using Microsoft.EntityFrameworkCore;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Persistence.Idempotency;

/// <summary>Durable backing store for <c>IdempotencyBehavior</c>.</summary>
public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly VesperaDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IdempotencyStore(VesperaDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        _dbContext.Set<IdempotencyRecordEntity>().AnyAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task MarkAsProcessedAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        await _dbContext.Set<IdempotencyRecordEntity>().AddAsync(
            new IdempotencyRecordEntity { IdempotencyKey = idempotencyKey, ProcessedAt = _dateTimeProvider.UtcNow }, cancellationToken);
}
