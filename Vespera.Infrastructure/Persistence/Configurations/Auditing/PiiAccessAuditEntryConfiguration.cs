using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Infrastructure.Persistence.Auditing;

namespace Vespera.Infrastructure.Persistence.Configurations.Auditing;

public sealed class PiiAccessAuditEntryConfiguration : IEntityTypeConfiguration<PiiAccessAuditEntry>
{
    public void Configure(EntityTypeBuilder<PiiAccessAuditEntry> builder)
    {
        builder.ToTable("PiiAccessAuditEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SubjectType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Field).IsRequired().HasMaxLength(128);
        builder.Property(e => e.AccessedBy).IsRequired().HasMaxLength(256);
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.SubjectType, e.SubjectId });
    }
}
