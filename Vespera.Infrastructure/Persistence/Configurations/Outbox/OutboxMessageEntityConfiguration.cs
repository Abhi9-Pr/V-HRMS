using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Infrastructure.Persistence.Outbox;

namespace Vespera.Infrastructure.Persistence.Configurations.Outbox;

public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Type).IsRequired().HasMaxLength(1024);
        builder.Property(e => e.Payload).IsRequired();
        builder.Property(e => e.OccurredOn).IsRequired();
        builder.Property(e => e.ProcessedAt);
        builder.Property(e => e.NextAttemptAt);
        builder.Property(e => e.Attempts).IsRequired();
        builder.Property(e => e.LastError);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(e => e.Status);
    }
}
