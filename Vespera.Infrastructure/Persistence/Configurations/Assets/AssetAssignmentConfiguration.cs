using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations.Assets;

/// <summary>AssetAssignment is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot (no
/// audit/soft-delete columns), so it's configured directly rather than via
/// TenantScopedEntityConfiguration&lt;,&gt; — mirrors ExpenseClaimConfiguration's shape.</summary>
public sealed class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AssetAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.AssetId).HasConversion(id => id.Value, value => new AssetId(value));
        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));

        builder.Property(e => e.AssignedAt).IsRequired();
        builder.Property(e => e.ReturnedAt);
        builder.Property(e => e.ReturnCondition).HasMaxLength(1024);
        builder.Property(e => e.HandoverSignatureReference).HasMaxLength(1024);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);

        // AssetConditionReport is a plain record with no natural key (Rating+Notes+RecordedAt
        // don't uniquely identify a row on their own, and duplicates are legitimate — e.g. two
        // "Good" reports on different dates). EF Core needs *some* key for an owned collection
        // table; a shadow Guid key configured here (not on the domain type, which stays a pure
        // value object) is the standard way to give it one.
        builder.OwnsMany(e => e.ConditionReports, reports =>
        {
            reports.ToTable("AssetConditionReports");
            reports.Property<Guid>("Id").ValueGeneratedOnAdd();
            reports.HasKey("Id");

            reports.Property(r => r.Rating).HasConversion<string>().HasMaxLength(32);
            reports.Property(r => r.Notes).HasMaxLength(1024);
            reports.Property(r => r.RecordedAt).IsRequired();
            reports.Property(r => r.RecordedBy).IsRequired().HasMaxLength(256);
        });
    }
}
