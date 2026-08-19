using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="IAuditable"/> Created/Modified fields from <see cref="IDateTimeProvider"/> and
/// <see cref="ICurrentUser"/>, and regenerates every changed <see cref="AggregateRoot{TId}.RowVersion"/>
/// — the app-managed concurrency token (see AggregateRoot&lt;TId&gt; for why it isn't a
/// provider-native rowversion/xmin column).
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUser _currentUser;

    public AuditableEntityInterceptor(IDateTimeProvider dateTimeProvider, ICurrentUser currentUser)
    {
        _dateTimeProvider = dateTimeProvider;
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        var actor = _currentUser.UserId?.ToString() ?? "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.Metadata.FindProperty(RowVersionPropertyName) is not null)
            {
                entry.Property(RowVersionPropertyName).CurrentValue = Guid.NewGuid().ToByteArray();
            }

            if (entry.Entity is not IAuditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue = actor;
            }
            else
            {
                entry.Property(nameof(IAuditable.ModifiedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.ModifiedBy)).CurrentValue = actor;
            }
        }
    }

    private const string RowVersionPropertyName = "RowVersion";
}
