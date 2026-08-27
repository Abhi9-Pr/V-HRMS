using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class CelebrationConfiguration : IEntityTypeConfiguration<Celebration>
{
    public void Configure(EntityTypeBuilder<Celebration> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new CelebrationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
        builder.Property(e => e.CelebrationType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Date).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.TenantId, e.Date });
    }
}
