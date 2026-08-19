using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared shape for every <see cref="AuditableTenantAggregateRoot{TId}"/>: the audit columns,
/// soft-delete columns, the app-managed <see cref="AggregateRoot{TId}.RowVersion"/> concurrency
/// token, and the tenant FK column itself. The <em>query filters</em> that make TenantId/IsDeleted
/// actually filter reads are applied once, generically, in <see cref="VesperaDbContext"/> —
/// they need a fresh closure over the ambient <c>ITenantContext</c> per DbContext instance, which
/// an assembly-scanned, parameterless-constructed <see cref="IEntityTypeConfiguration{TEntity}"/>
/// has no way to receive. This class covers everything else, so a concrete configuration only
/// has to describe what makes that one entity different (<see cref="ConfigureEntity"/>).
/// </summary>
public abstract class TenantScopedEntityConfiguration<TEntity, TId> : IEntityTypeConfiguration<TEntity>
    where TEntity : AuditableTenantAggregateRoot<TId>
    where TId : notnull
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ModifiedAt);
        builder.Property(e => e.ModifiedBy).HasMaxLength(256);

        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy).HasMaxLength(256);

        builder.Ignore(e => e.DomainEvents);

        ConfigureEntity(builder);
    }

    /// <summary>
    /// Must at minimum call <c>builder.HasKey(e => e.Id)</c> and configure the typed Id's
    /// conversion to/from <see cref="Guid"/> — the Id's CLR type differs per entity, so it can't
    /// be generalized here (see CONTRIBUTING-slices.md: typed IDs are mapped explicitly).
    /// </summary>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
