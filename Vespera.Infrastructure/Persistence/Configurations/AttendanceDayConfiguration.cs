using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="AttendanceDay"/> is a plain <c>AggregateRoot&lt;TId&gt;</c> + <c>ITenantScoped</c> —
/// not an <c>AuditableTenantAggregateRoot</c> (no soft-delete, no audit stamps) — so it's
/// configured directly, the same way <see cref="ShiftRosterConfiguration"/> is. The tenant query
/// filter is still applied centrally in <see cref="VesperaDbContext"/> (it only needs
/// <c>ITenantScoped</c>, not either base configuration class).
/// </summary>
public sealed class AttendanceDayConfiguration : IEntityTypeConfiguration<AttendanceDay>
{
    public void Configure(EntityTypeBuilder<AttendanceDay> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AttendanceDayId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EmployeeId)
            .HasConversion(id => id.Value, value => new EmployeeId(value))
            .IsRequired();
        builder.Property(e => e.Date).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.Date }).IsUnique();

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(e => e.FirstIn);
        builder.Property(e => e.LastOut);
        builder.Property(e => e.WorkedMinutes).IsRequired();
        builder.Property(e => e.LateByMinutes).IsRequired();
        builder.Property(e => e.EarlyLeaveByMinutes).IsRequired();
        builder.Property(e => e.OvertimeMinutes).IsRequired();
        builder.Property(e => e.IsLopCandidate).IsRequired();
        builder.Property(e => e.LastComputedAt);
        builder.Property(e => e.LastComputedBy).HasMaxLength(256);
        builder.Property(e => e.LastChangedAt).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.IsOpen);
        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.WorkedTime);

        // ISoftDeletable (only implemented so AttendanceDay can be a DeltaSyncQueryHandlerBase
        // TEntity — see the property doc comments) needs a real mapped column, not an Ignore()'d
        // computed one: VesperaDbContext's shared soft-delete query filter convention builds
        // `!e.IsDeleted` as a translatable property-access expression for every ISoftDeletable
        // type, which an ignored/unmapped property can't satisfy.
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy).HasMaxLength(256);

        builder.OwnsMany(e => e.Punches, punches =>
        {
            punches.ToTable("AttendancePunches");
            punches.HasKey(p => p.Id);
            punches.Property(p => p.Id)
                .HasConversion(id => id.Value, value => new AttendancePunchId(value))
                .ValueGeneratedNever();
            punches.Property(p => p.PunchType).HasConversion<string>().HasMaxLength(16).IsRequired();
            punches.Property(p => p.PunchedAtUtc).IsRequired();
            punches.Property(p => p.Source).HasConversion<string>().HasMaxLength(16).IsRequired();
            punches.Property(p => p.RequiresApproval).IsRequired();
            punches.Property(p => p.FlagReason).HasMaxLength(200);

            // Same plain-scalar-conversion reasoning as GeofenceZoneConfiguration.Center — an
            // owned-navigation-typed property can't be bound via the owner's constructor, and
            // GeoCoordinate has no settable properties. Nullable here (a punch may carry no
            // location at all, e.g. a manager-entered correction).
            punches.Property(p => p.Location)
                .HasConversion(
                    coordinate => coordinate == null ? null : $"{coordinate.Latitude}|{coordinate.Longitude}",
                    value => ParseCoordinate(value))
                .HasMaxLength(64);
        });
    }

    private static GeoCoordinate? ParseCoordinate(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var parts = value.Split('|');
        return GeoCoordinate.Create(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture)).Value;
    }
}
