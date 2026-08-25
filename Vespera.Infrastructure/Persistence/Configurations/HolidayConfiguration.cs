using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class HolidayConfiguration : TenantScopedEntityConfiguration<Holiday, HolidayId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Holiday> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new HolidayId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.LocationId)
            .HasConversion(id => id.Value, value => new LocationId(value))
            .IsRequired();

        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
    }
}
