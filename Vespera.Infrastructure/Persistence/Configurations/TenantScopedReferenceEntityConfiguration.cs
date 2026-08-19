using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared shape for <see cref="EffectiveDated{TId}"/> reference data that is tenant-scoped but not
/// a full audited aggregate root (no <see cref="AggregateRoot{TId}.RowVersion"/>, no audit stamps,
/// no domain events — e.g. <c>LeavePolicy</c>, <c>StatutoryRuleSet</c>, <c>ReportingRelationship</c>).
/// Mirrors <see cref="TenantScopedEntityConfiguration{TEntity,TId}"/>'s split: the tenant/soft-delete
/// query filters are applied centrally in <see cref="VesperaDbContext"/>, not here.
/// </summary>
public abstract class TenantScopedReferenceEntityConfiguration<TEntity, TId> : IEntityTypeConfiguration<TEntity>
    where TEntity : EffectiveDated<TId>, ITenantScoped
    where TId : notnull
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.ValidFrom).IsRequired();
        builder.Property(e => e.ValidTo);

        ConfigureEntity(builder);
    }

    /// <summary>Must at minimum call <c>builder.HasKey(e => e.Id)</c> and configure the typed Id's conversion.</summary>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
