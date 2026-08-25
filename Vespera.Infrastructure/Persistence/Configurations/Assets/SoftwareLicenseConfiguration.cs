using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Assets;

namespace Vespera.Infrastructure.Persistence.Configurations.Assets;

public sealed class SoftwareLicenseConfiguration : TenantScopedEntityConfiguration<SoftwareLicense, SoftwareLicenseId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SoftwareLicense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new SoftwareLicenseId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ProductName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.SeatCount).IsRequired();
        builder.Property(e => e.SeatsUsed).IsRequired();
        builder.Property(e => e.ExpiresAt);
    }
}
