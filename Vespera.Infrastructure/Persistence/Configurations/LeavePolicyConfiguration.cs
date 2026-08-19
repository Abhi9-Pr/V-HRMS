using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Leave;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class LeavePolicyConfiguration : TenantScopedReferenceEntityConfiguration<LeavePolicy, LeavePolicyId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LeavePolicy> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LeavePolicyId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.LeaveTypeId).HasConversion(id => id.Value, value => new LeaveTypeId(value));
        builder.Property(e => e.AnnualEntitlementDays).HasPrecision(9, 2);
        builder.Property(e => e.AccrualRatePerMonth).HasPrecision(9, 2);
        builder.Property(e => e.MaxCarryForwardDays).HasPrecision(9, 2);
    }
}
