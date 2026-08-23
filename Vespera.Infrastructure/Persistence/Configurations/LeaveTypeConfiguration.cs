using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Leave;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class LeaveTypeConfiguration : TenantScopedEntityConfiguration<LeaveType, LeaveTypeId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LeaveType> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LeaveTypeId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
        builder.Property(e => e.IsPaid).IsRequired();
        builder.Property(e => e.CarryForwardLimit).HasPrecision(9, 2);

        builder.Property(e => e.ApplicableGender).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.MinimumTenureMonths).IsRequired();
        builder.Property(e => e.IsEncashable).IsRequired();
        builder.Property(e => e.MaxEncashableDays).HasPrecision(9, 2);
    }
}
