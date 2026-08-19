using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Infrastructure.Persistence.Idempotency;

namespace Vespera.Infrastructure.Persistence.Configurations.Idempotency;

public sealed class IdempotencyRecordEntityConfiguration : IEntityTypeConfiguration<IdempotencyRecordEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecordEntity> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(e => e.IdempotencyKey);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(256).ValueGeneratedNever();

        builder.Property(e => e.ProcessedAt).IsRequired();
    }
}
