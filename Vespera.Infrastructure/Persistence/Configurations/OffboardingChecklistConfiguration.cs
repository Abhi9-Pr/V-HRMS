using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class OffboardingChecklistConfiguration : TenantScopedEntityConfiguration<OffboardingChecklist, OffboardingChecklistId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<OffboardingChecklist> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new OffboardingChecklistId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.EmployeeId)
            .HasConversion(id => id.Value, value => new EmployeeId(value))
            .IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.EmployeeId }).IsUnique();

        builder.Property(c => c.ExitDate).IsRequired();

        builder.Property(c => c.AccessRevokedStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.AccessRevokedAt);

        builder.Property(c => c.AssetsRecoveredStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.AssetsRecoveredAt);

        builder.Property(c => c.FinalSettlementStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.FinalSettlementAt);
    }
}
