using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class ShiftConfiguration : TenantScopedEntityConfiguration<Shift, ShiftId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Shift> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ShiftId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.EndTime).IsRequired();
        builder.Property(e => e.GraceMinutes).IsRequired();
    }
}
