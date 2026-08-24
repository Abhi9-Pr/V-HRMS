using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations.Assets;

public sealed class OffboardingChecklistConfiguration : IEntityTypeConfiguration<OffboardingChecklist>
{
    public void Configure(EntityTypeBuilder<OffboardingChecklist> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new OffboardingChecklistId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.IsComplete);

        // OffboardingChecklistItem is a plain value object (Description+IsComplete) with no
        // natural key — same shadow-key approach as AssetConditionReport in
        // AssetAssignmentConfiguration, kept out of the domain type itself.
        builder.OwnsMany(e => e.Items, items =>
        {
            items.ToTable("OffboardingChecklistItems");
            items.Property<Guid>("Id").ValueGeneratedOnAdd();
            items.HasKey("Id");

            items.Property(i => i.Description).IsRequired().HasMaxLength(512);
            items.Property(i => i.IsComplete).IsRequired();
        });
    }
}
