using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations.Assets;

public sealed class SoftwareLicenseAllocationConfiguration : IEntityTypeConfiguration<SoftwareLicenseAllocation>
{
    public void Configure(EntityTypeBuilder<SoftwareLicenseAllocation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new SoftwareLicenseAllocationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.LicenseId).HasConversion(id => id.Value, value => new SoftwareLicenseId(value));
        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));

        builder.Property(e => e.AllocatedAt).IsRequired();
        builder.Property(e => e.ReleasedAt);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }
}
