using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Payroll;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class SalaryComponentConfiguration : TenantScopedEntityConfiguration<SalaryComponent, SalaryComponentId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new SalaryComponentId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ComponentType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.IsTaxable).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
    }
}
