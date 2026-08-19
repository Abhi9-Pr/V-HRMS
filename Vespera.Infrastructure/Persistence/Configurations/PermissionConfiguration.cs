using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>A global, non-tenant-scoped catalog entry — no audit stamps, no soft-delete, no tenant filter.</summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new PermissionId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Code).IsRequired().HasMaxLength(128);
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Description).IsRequired().HasMaxLength(512);
    }
}
