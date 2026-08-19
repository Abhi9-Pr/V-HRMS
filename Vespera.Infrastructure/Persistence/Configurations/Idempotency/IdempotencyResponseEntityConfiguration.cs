using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Infrastructure.Persistence.Idempotency;

namespace Vespera.Infrastructure.Persistence.Configurations.Idempotency;

public sealed class IdempotencyResponseEntityConfiguration : IEntityTypeConfiguration<IdempotencyResponseEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyResponseEntity> builder)
    {
        builder.ToTable("IdempotencyResponses");
        builder.HasKey(e => e.IdempotencyKey);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(256).ValueGeneratedNever();

        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();

        builder.HasIndex(e => e.ExpiresAt);
    }
}
