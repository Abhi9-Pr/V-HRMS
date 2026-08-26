using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class TaxRegimeVersionConfiguration : IEntityTypeConfiguration<TaxRegimeVersion>
{
    public void Configure(EntityTypeBuilder<TaxRegimeVersion> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new TaxRegimeVersionId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.RegimeType).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.FinancialYear).IsRequired().HasMaxLength(16);
        builder.HasIndex(e => new { e.TenantId, e.RegimeType, e.FinancialYear }).IsUnique();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        // TaxSlab (a ValueObject with no Id of its own) gets EF's default shadow key for owned
        // collections — no HasKey call needed, same as every other pure-value owned collection.
        builder.OwnsMany(e => e.Slabs, slabs =>
        {
            slabs.ToTable("TaxSlabs");
            slabs.Property(s => s.UpTo)
                .HasConversion(
                    upTo => $"{upTo.Amount.ToString(CultureInfo.InvariantCulture)}|{upTo.Currency}",
                    value => ParseMoney(value))
                .HasMaxLength(64);
            slabs.Property(s => s.RatePercent).HasPrecision(5, 2);
        });
    }

    private static Money ParseMoney(string value)
    {
        var parts = value.Split('|');
        return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
    }
}
