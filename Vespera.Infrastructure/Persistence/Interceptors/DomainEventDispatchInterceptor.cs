using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Infrastructure.Persistence.Outbox;

namespace Vespera.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Implements the transactional outbox for raised domain events: every tracked
/// <see cref="IHasDomainEvents"/> entity with pending events gets one <see cref="OutboxMessageEntity"/>
/// row per event, added to the same <c>DbContext</c> before the physical write — so it commits in
/// the exact same <c>SaveChangesAsync</c> call, and therefore the same transaction, as the entity
/// change that raised the event. <see cref="BackgroundJobs.OutboxDispatcherHostedService"/> reads
/// and publishes these rows afterwards, out of band.
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public DomainEventDispatchInterceptor(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        DispatchEvents(eventData.Context, _correlationIdProvider.Current);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        DispatchEvents(eventData.Context, _correlationIdProvider.Current);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void DispatchEvents(DbContext? context, string? correlationId)
    {
        if (context is null)
        {
            return;
        }

        var entitiesWithEvents = context.ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .OfType<IHasDomainEvents>()
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var eventType = domainEvent.GetType();
                context.Add(new OutboxMessageEntity
                {
                    Id = Guid.NewGuid(),
                    CorrelationId = correlationId,
                    Type = eventType.AssemblyQualifiedName!,
                    Payload = JsonSerializer.Serialize(domainEvent, eventType),
                    OccurredOn = domainEvent.OccurredOn,
                    Status = OutboxMessageStatus.Pending,
                });
            }

            entity.ClearDomainEvents();
        }
    }
}
