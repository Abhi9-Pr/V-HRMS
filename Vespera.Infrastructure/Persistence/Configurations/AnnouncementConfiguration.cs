using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class AnnouncementConfiguration : TenantScopedEntityConfiguration<Announcement, AnnouncementId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AnnouncementId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.AudienceScope).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(e => e.TargetDepartmentId)
            .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new DepartmentId(value.Value) : (DepartmentId?)null);

        builder.Property(e => e.TargetLocationId)
            .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new LocationId(value.Value) : (LocationId?)null);

        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.PublishAt).IsRequired();
        builder.Property(e => e.ExpiresAt);
        builder.Property(e => e.IsPublished).IsRequired();
        builder.Property(e => e.IsPinned).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.IsPublished, e.PublishAt });
    }
}
