using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Infrastructure.Persistence.Configurations.Expense;

/// <summary>CurrencyRate is Entity+ITenantScoped (not an AggregateRoot/AuditableTenantAggregateRoot)
/// but still gets its own table since it's read/written directly via IReadRepository/IWriteRepository
/// — the "cached daily-rate store". <see cref="Persistence.VesperaDbContext.ApplyGlobalQueryFilters"/>
/// still applies the tenant filter automatically since it checks ITenantScoped independently of the
/// aggregate-root hierarchy.</summary>
public sealed class CurrencyRateConfiguration : IEntityTypeConfiguration<CurrencyRate>
{
    public void Configure(EntityTypeBuilder<CurrencyRate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new CurrencyRateId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.FromCurrency).HasConversion<string>().HasMaxLength(8);
        builder.Property(e => e.ToCurrency).HasConversion<string>().HasMaxLength(8);
        builder.Property(e => e.Rate).HasPrecision(18, 6);
        builder.Property(e => e.EffectiveDate).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.FromCurrency, e.ToCurrency, e.EffectiveDate }).IsUnique();
    }
}
