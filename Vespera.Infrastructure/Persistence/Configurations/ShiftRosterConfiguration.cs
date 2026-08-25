using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ShiftRoster"/> is a plain <c>AggregateRoot&lt;TId&gt;</c> + <c>ITenantScoped</c> — not
/// an <c>AuditableTenantAggregateRoot</c> (no soft-delete, no audit stamps beyond its own
/// <see cref="ShiftRoster.CreatedAt"/>) and not <c>EffectiveDated</c> either, so neither
/// <see cref="TenantScopedEntityConfiguration{TEntity,TId}"/> nor
/// <see cref="TenantScopedReferenceEntityConfiguration{TEntity,TId}"/> fits; configured directly.
/// The tenant query filter itself is still applied centrally in <see cref="VesperaDbContext"/>
/// (it only needs <c>ITenantScoped</c>, not either base class).
/// </summary>
public sealed class ShiftRosterConfiguration : IEntityTypeConfiguration<ShiftRoster>
{
    public void Configure(EntityTypeBuilder<ShiftRoster> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ShiftRosterId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.ShiftId).HasConversion(id => id.Value, value => new ShiftId(value));

        // A plain scalar conversion, not OwnsOne: EF Core can't bind an owned-navigation-typed
        // constructor parameter when materializing the owner via its (required, private)
        // constructor — same reasoning as LocationConfiguration's Coordinate column.
        builder.Property(e => e.Period)
            .HasConversion(
                period => $"{period.Start:O}|{period.End:O}",
                value => ParsePeriod(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.IsOverride).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.PublishedAt);
        builder.Property(e => e.PublishedBy).HasMaxLength(256);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });
    }

    private static DateRange ParsePeriod(string value)
    {
        var parts = value.Split('|');
        var start = DateOnly.ParseExact(parts[0], "O", CultureInfo.InvariantCulture);
        var end = DateOnly.ParseExact(parts[1], "O", CultureInfo.InvariantCulture);
        return DateRange.Create(start, end).Value;
    }
}
