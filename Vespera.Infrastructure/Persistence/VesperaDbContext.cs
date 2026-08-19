using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence;

public sealed class VesperaDbContext : DbContext, IVesperaDbContext, IUnitOfWork
{
    private readonly ITenantContext _tenantContext;
    private readonly IPiiProtector _piiProtector;

    public VesperaDbContext(DbContextOptions<VesperaDbContext> options, ITenantContext tenantContext, IPiiProtector piiProtector)
        : base(options)
    {
        _tenantContext = tenantContext;
        _piiProtector = piiProtector;
    }

    IQueryable<TEntity> IVesperaDbContext.Set<TEntity>() => Set<TEntity>();

    /// <summary>Translates EF's provider-specific concurrency exception into
    /// <see cref="ConcurrencyConflictException"/> — the only concurrency-related type
    /// Application is allowed to reference. See <c>TransactionBehavior</c>.</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("A concurrency conflict occurred while saving changes.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);

        // These two steps need a runtime service instance (IPiiProtector / the ambient
        // ITenantContext) that an assembly-scanned, parameterless-constructed
        // IEntityTypeConfiguration<T> has no way to receive — see TenantScopedEntityConfiguration
        // and EmployeeConfiguration's doc comments for the full rationale.
        ConfigureEmployeeProtectedFields(modelBuilder);
        ApplyGlobalQueryFilters(modelBuilder);
    }

    private void ConfigureEmployeeProtectedFields(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(builder =>
        {
            builder.Property(e => e.Pan)
                .HasConversion(
                    pan => pan == null ? null : _piiProtector.Protect(pan.Value),
                    value => value == null ? null : PanNumber.Create(_piiProtector.Unprotect(value)).Value)
                .HasMaxLength(1024);

            builder.Property(e => e.BankAccount)
                .HasConversion(
                    account => account == null ? null : _piiProtector.Protect(account.Value),
                    value => value == null ? null : BankAccountNumber.Create(_piiProtector.Unprotect(value)).Value)
                .HasMaxLength(1024);
        });
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        var ambientTenantId = _tenantContext.HasTenant ? _tenantContext.TenantId : default;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenantScoped = typeof(ITenantScoped).IsAssignableFrom(clrType);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);

            if (!isTenantScoped && !isSoftDeletable)
            {
                continue;
            }

            var parameter = Expression.Parameter(clrType, "e");
            Expression? body = null;

            if (isSoftDeletable)
            {
                body = Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
            }

            if (isTenantScoped)
            {
                var tenantMatches = Expression.Equal(
                    Expression.Property(parameter, nameof(ITenantScoped.TenantId)), Expression.Constant(ambientTenantId));
                body = body is null ? tenantMatches : Expression.AndAlso(body, tenantMatches);
            }

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(body!, parameter));
        }
    }
}
