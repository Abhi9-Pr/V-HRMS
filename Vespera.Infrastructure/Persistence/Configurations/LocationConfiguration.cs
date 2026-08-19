using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class LocationConfiguration : TenantScopedEntityConfiguration<Location, LocationId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LocationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.AddressLine).HasMaxLength(512);
        builder.Property(e => e.City).HasMaxLength(128);
        builder.Property(e => e.Country).HasMaxLength(128);
        builder.Property(e => e.TimeZoneId).IsRequired().HasMaxLength(64);

        // A plain scalar conversion, not OwnsOne: EF Core can't bind an owned-navigation-typed
        // constructor parameter when materializing the owner via its (required, private)
        // constructor, and GeoCoordinate has no settable properties for EF to populate post-construction.
        builder.Property(e => e.Coordinate)
            .HasConversion(
                coordinate => $"{coordinate.Latitude}|{coordinate.Longitude}",
                value => ParseCoordinate(value))
            .HasMaxLength(64);
    }

    private static GeoCoordinate ParseCoordinate(string value)
    {
        var parts = value.Split('|');
        return GeoCoordinate.Create(double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)).Value;
    }
}
