using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Infrastructure.Persistence.Auditing;

namespace Vespera.Infrastructure.Persistence.Configurations.Auditing;

public sealed class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.ActionType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.EntityName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.EntityKey).IsRequired().HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.OldValueJson);
        builder.Property(e => e.NewValueJson);

        builder.HasIndex(e => new { e.TenantId, e.EntityName, e.EntityKey });
    }
}
