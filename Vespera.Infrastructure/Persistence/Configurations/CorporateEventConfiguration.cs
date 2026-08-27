using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Workspace;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class CorporateEventConfiguration : TenantScopedEntityConfiguration<CorporateEvent, CorporateEventId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CorporateEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new CorporateEventId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.StartsAt).IsRequired();
        builder.Property(e => e.EndsAt).IsRequired();
        builder.Property(e => e.LocationText).IsRequired().HasMaxLength(256);
        builder.Property(e => e.IsCancelled).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.StartsAt });
    }
}
