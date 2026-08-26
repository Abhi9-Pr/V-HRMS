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

        builder.Property(e => e.AccrualFrequency).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.MinimumTenureMonthsForAccrual).IsRequired();
        builder.Property(e => e.RequiresSkipLevelApproval).IsRequired();
        builder.Property(e => e.SkipLevelThresholdDays).HasPrecision(9, 2);
        builder.Property(e => e.RequiresHrApproval).IsRequired();
        builder.Property(e => e.NegativeBalancePolicy).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.MaxNegativeBalanceDays).HasPrecision(9, 2);
        builder.Property(e => e.SandwichLeaveEnabled).IsRequired();

        builder.OwnsMany(e => e.TenureAccrualTiers, tiers =>
        {
            tiers.ToTable("LeavePolicyTenureAccrualTiers");
            tiers.WithOwner().HasForeignKey("LeavePolicyId");
            tiers.Property<int>("Id");
            tiers.HasKey("Id");
            tiers.Property(t => t.MinimumTenureMonths).IsRequired();
            tiers.Property(t => t.MonthlyRate).HasPrecision(9, 2);
        });
    }
}
