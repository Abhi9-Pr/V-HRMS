using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class DesignationConfiguration : TenantScopedEntityConfiguration<Designation, DesignationId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Designation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new DesignationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Grade).IsRequired();
    }
}
