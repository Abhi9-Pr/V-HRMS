using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class BiometricDeviceConfiguration : TenantScopedEntityConfiguration<BiometricDevice, BiometricDeviceId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BiometricDevice> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new BiometricDeviceId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.LocationId)
            .HasConversion(id => id.Value, value => new LocationId(value))
            .IsRequired();
        builder.HasIndex(e => e.LocationId);

        builder.Property(e => e.VendorType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Host).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Port).IsRequired();
        builder.Property(e => e.ApiKeyConfigurationKey).HasMaxLength(256);
        builder.Property(e => e.Cursor).HasMaxLength(512);
        builder.Property(e => e.IsActive).IsRequired();
    }
}
