using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : TenantScopedEntityConfiguration<Department, DepartmentId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new DepartmentId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Code).IsRequired().HasMaxLength(64);

        // Cross-aggregate references (own aggregate, or Employee) are stored as plain columns,
        // never as EF navigations/FKs — aggregates are loaded and saved independently.
        builder.Property(e => e.ParentDepartmentId)
            .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new DepartmentId(value.Value) : (DepartmentId?)null);

        builder.Property(e => e.HeadEmployeeId)
            .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new EmployeeId(value.Value) : (EmployeeId?)null);
    }
}
