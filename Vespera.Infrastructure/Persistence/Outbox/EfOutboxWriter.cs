using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Persistence.Outbox;

/// <summary>
/// Explicit-write path for outbox messages that aren't tied to a raised Domain event (e.g. a
/// handler that wants to enqueue an integration message directly). Stages the row; like every
/// other write in this codebase, <c>TransactionBehavior</c> is what actually calls
/// <c>SaveChangesAsync</c>. The automatic path — one row per raised <c>DomainEvent</c> — is
/// <see cref="Interceptors.DomainEventDispatchInterceptor"/>, not this class.
/// </summary>
public sealed class EfOutboxWriter : IOutboxWriter
{
    private readonly VesperaDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public EfOutboxWriter(VesperaDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task WriteAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        await _dbContext.Set<OutboxMessageEntity>().AddAsync(
            new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.HasTenant ? _tenantContext.TenantId.Value : null,
                Type = message.Type,
                Payload = message.Payload,
                OccurredOn = message.OccurredOn,
                Status = OutboxMessageStatus.Pending,
            },
            cancellationToken);
}
