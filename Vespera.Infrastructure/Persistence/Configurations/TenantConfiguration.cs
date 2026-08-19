using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="Tenant"/> is the one aggregate that is never itself tenant-scoped, so it doesn't fit
/// <see cref="TenantScopedEntityConfiguration{TEntity,TId}"/> — configured directly instead.
/// </summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Code).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ModifiedAt);
        builder.Property(e => e.ModifiedBy).HasMaxLength(256);

        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy).HasMaxLength(256);

        builder.Ignore(e => e.DomainEvents);
    }
}
