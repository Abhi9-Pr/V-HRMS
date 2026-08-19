using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>A plain <c>AggregateRoot&lt;TId&gt; + ITenantScoped</c> (not
/// <c>AuditableTenantAggregateRoot</c> — a token has no meaningful "modified by", and
/// revocation/rotation timestamps already capture its lifecycle), so this is a direct
/// configuration rather than derived from either shared base.</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new RefreshTokenId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(e => e.TokenHash).IsRequired().HasMaxLength(256);
        builder.HasIndex(e => e.TokenHash).IsUnique();

        builder.Property(e => e.DeviceId).IsRequired().HasMaxLength(256);
        builder.Property(e => e.FamilyId).IsRequired();
        builder.HasIndex(e => e.FamilyId);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.RevokedAt);

        builder.Property(e => e.ReplacedByTokenId)
            .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new RefreshTokenId(value.Value) : (RefreshTokenId?)null);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }
}
