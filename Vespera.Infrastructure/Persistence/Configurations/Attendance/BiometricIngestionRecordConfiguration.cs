using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Infrastructure.Attendance;

namespace Vespera.Infrastructure.Persistence.Configurations.Attendance;

public sealed class BiometricIngestionRecordConfiguration : IEntityTypeConfiguration<BiometricIngestionRecord>
{
    public void Configure(EntityTypeBuilder<BiometricIngestionRecord> builder)
    {
        builder.ToTable("BiometricIngestionRecords");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ExternalRecordId).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ProcessedAt).IsRequired();

        builder.HasIndex(e => new { e.BiometricDeviceId, e.ExternalRecordId }).IsUnique();
    }
}
