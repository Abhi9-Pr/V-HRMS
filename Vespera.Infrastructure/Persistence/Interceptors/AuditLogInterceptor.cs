using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence.Auditing;
using Vespera.Infrastructure.Persistence.Idempotency;
using Vespera.Infrastructure.Persistence.Outbox;

namespace Vespera.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes one <see cref="AuditLogEntity"/> row per changed entity. Any property whose declared
/// CLR type implements <see cref="IPersonalData"/>, or that carries <see cref="PiiAttribute"/>, is
/// redacted to a SHA-256 hash before being written — the clear value never reaches the audit
/// table. Runs during <c>SavingChanges</c> (not <c>SavedChanges</c>) because it needs to read
/// pending changes off the change tracker before they're written; the new <see cref="AuditLogEntity"/>
/// rows it adds ride along in the same <c>SaveChangesAsync</c> call, so they land in the same
/// transaction as the change they describe.
/// </summary>
public sealed class AuditLogInterceptor : SaveChangesInterceptor
{
    private static readonly Type[] ExcludedEntityTypes = [typeof(AuditLogEntity), typeof(OutboxMessageEntity), typeof(IdempotencyRecordEntity)];

    private readonly ICurrentUser _currentUser;

    public AuditLogInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteAuditRows(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        WriteAuditRows(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteAuditRows(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var candidates = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => !ExcludedEntityTypes.Contains(entry.Entity.GetType()))
            .ToList();

        foreach (var entry in candidates)
        {
            context.Add(BuildAuditLog(entry));
        }
    }

    private AuditLogEntity BuildAuditLog(EntityEntry entry)
    {
        var (oldValues, newValues) = entry.State switch
        {
            EntityState.Added => ((Dictionary<string, object?>?)null, SnapshotValues(entry, useOriginalValues: false)),
            EntityState.Deleted => (SnapshotValues(entry, useOriginalValues: true), (Dictionary<string, object?>?)null),
            _ => (SnapshotChangedValues(entry, useOriginalValues: true), SnapshotChangedValues(entry, useOriginalValues: false)),
        };

        return new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = entry.Entity is ITenantScoped scoped ? scoped.TenantId.Value : null,
            UserId = _currentUser.UserId,
            Timestamp = DateTimeOffset.UtcNow,
            ActionType = entry.State switch
            {
                EntityState.Added => AuditActionType.Created,
                EntityState.Deleted => AuditActionType.Deleted,
                _ => AuditActionType.Modified,
            },
            EntityName = entry.Entity.GetType().Name,
            EntityKey = BuildEntityKey(entry),
            IpAddress = _currentUser.IpAddress,
            OldValueJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValueJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
        };
    }

    private static string BuildEntityKey(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties ?? [];
        var values = keyProperties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty);
        return string.Join(",", values);
    }

    private static Dictionary<string, object?> SnapshotValues(EntityEntry entry, bool useOriginalValues)
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            result[property.Metadata.Name] = RedactIfPii(property, useOriginalValues ? property.OriginalValue : property.CurrentValue);
        }

        return result;
    }

    private static Dictionary<string, object?> SnapshotChangedValues(EntityEntry entry, bool useOriginalValues)
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            result[property.Metadata.Name] = RedactIfPii(property, useOriginalValues ? property.OriginalValue : property.CurrentValue);
        }

        return result;
    }

    private static object? RedactIfPii(PropertyEntry property, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var clrType = property.Metadata.PropertyInfo?.PropertyType ?? property.Metadata.FieldInfo?.FieldType;
        var isPii = clrType is not null &&
            (typeof(IPersonalData).IsAssignableFrom(clrType) ||
             property.Metadata.PropertyInfo?.GetCustomAttributes(typeof(PiiAttribute), inherit: true).Length > 0 ||
             property.Metadata.FieldInfo?.GetCustomAttributes(typeof(PiiAttribute), inherit: true).Length > 0);

        return isPii ? Hash(value.ToString() ?? string.Empty) : value;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
