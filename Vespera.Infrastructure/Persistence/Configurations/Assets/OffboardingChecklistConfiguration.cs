using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using OffboardingChecklist = Vespera.Domain.Assets.OffboardingChecklist;
using OffboardingChecklistId = Vespera.Domain.Assets.OffboardingChecklistId;

namespace Vespera.Infrastructure.Persistence.Configurations.Assets;

public sealed class OffboardingChecklistConfiguration : IEntityTypeConfiguration<OffboardingChecklist>
{
    public void Configure(EntityTypeBuilder<OffboardingChecklist> builder)
    {
        // Explicit table name: Vespera.Domain.Eis.OffboardingChecklist (the Phase 8 employee
        // exit checklist — access revocation/final settlement/high-level "assets recovered"
        // sign-off) is a distinct aggregate that otherwise maps to the same default "Offboarding
        // Checklist" table name by convention (same CLR type name, different namespace). That one
        // already has real Postgres migrations under that name, so this one moves instead.
        builder.ToTable("AssetOffboardingChecklists");

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
            items.ToTable("AssetOffboardingChecklistItems");
            items.Property<Guid>("Id").ValueGeneratedOnAdd();
            items.HasKey("Id");

            items.Property(i => i.Description).IsRequired().HasMaxLength(512);
            items.Property(i => i.IsComplete).IsRequired();
        });
    }
}
