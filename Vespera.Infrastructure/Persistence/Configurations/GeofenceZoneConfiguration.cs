using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class GeofenceZoneConfiguration : TenantScopedEntityConfiguration<GeofenceZone, GeofenceZoneId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GeofenceZone> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new GeofenceZoneId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.LocationId)
            .HasConversion(id => id.Value, value => new LocationId(value))
            .IsRequired();
        builder.HasIndex(e => e.LocationId);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.RadiusMetres).IsRequired();

        // A plain scalar conversion, not OwnsOne: EF Core can't bind an owned-navigation-typed
        // constructor parameter when materializing the owner via its (required, private)
        // constructor — same reasoning as LocationConfiguration's Coordinate column.
        builder.Property(e => e.Center)
            .HasConversion(
                coordinate => $"{coordinate.Latitude}|{coordinate.Longitude}",
                value => ParseCoordinate(value))
            .HasMaxLength(64)
            .IsRequired();
    }

    private static GeoCoordinate ParseCoordinate(string value)
    {
        var parts = value.Split('|');
        return GeoCoordinate.Create(
            double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)).Value;
    }
}
