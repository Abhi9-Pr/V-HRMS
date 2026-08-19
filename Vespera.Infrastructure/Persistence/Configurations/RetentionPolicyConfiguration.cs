using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Compliance;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class RetentionPolicyConfiguration : TenantScopedEntityConfiguration<RetentionPolicy, RetentionPolicyId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new RetentionPolicyId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.EntityCategory).IsRequired().HasMaxLength(128);
        builder.HasIndex(e => new { e.TenantId, e.EntityCategory }).IsUnique();

        builder.Property(e => e.RetentionPeriodDays).IsRequired();
        builder.Property(e => e.Action).HasConversion<string>().HasMaxLength(32);
    }
}
