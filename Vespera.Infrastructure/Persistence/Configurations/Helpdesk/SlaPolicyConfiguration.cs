using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Helpdesk;

namespace Vespera.Infrastructure.Persistence.Configurations.Helpdesk;

public sealed class SlaPolicyConfiguration : TenantScopedEntityConfiguration<SlaPolicy, SlaPolicyId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new SlaPolicyId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ResponseTime).IsRequired();
        builder.Property(e => e.ResolutionTime).IsRequired();
        builder.Property(e => e.BusinessHoursStart).IsRequired();
        builder.Property(e => e.BusinessHoursEnd).IsRequired();
    }
}
