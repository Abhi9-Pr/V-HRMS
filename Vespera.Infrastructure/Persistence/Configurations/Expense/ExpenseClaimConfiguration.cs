using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations.Expense;

/// <summary>ExpenseClaim is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot (no
/// audit/soft-delete columns), so it's configured directly rather than via
/// TenantScopedEntityConfiguration&lt;,&gt; — mirrors that base's TenantId/RowVersion handling
/// by hand.</summary>
public sealed class ExpenseClaimConfiguration : IEntityTypeConfiguration<ExpenseClaim>
{
    public void Configure(EntityTypeBuilder<ExpenseClaim> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ExpenseClaimId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.SettlementCurrency).HasConversion<string>().HasMaxLength(8);
        builder.Property(e => e.RejectionReason).HasMaxLength(1024);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);

        builder.OwnsMany(e => e.Lines, lines =>
        {
            lines.ToTable("ExpenseLines");
            lines.HasKey(l => l.Id);
            lines.Property(l => l.Id)
                .HasConversion(id => id.Value, value => new ExpenseLineId(value))
                .ValueGeneratedNever();

            lines.Property(l => l.Category).IsRequired().HasMaxLength(100);
            lines.Property(l => l.Amount).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
            lines.Property(l => l.ExpenseDate).IsRequired();
            lines.Property(l => l.ReceiptReference).HasMaxLength(1024);
            lines.Property(l => l.Vendor).HasMaxLength(256);
            lines.Property(l => l.TaxAmount).HasConversion(NullableMoneyConverter.Instance).HasMaxLength(64);
            lines.Property(l => l.ConvertedAmount).HasConversion(NullableMoneyConverter.Instance).HasMaxLength(64);
            lines.Property(l => l.ExchangeRate).HasPrecision(18, 6);
        });
    }

    // Plain scalar conversions, not OwnsOne — see PayrollRunConfiguration for why: the owning
    // entity's constructor binds Money parameters directly, and EF can't bind an owned-navigation
    // -typed constructor parameter.
    private static class MoneyConverter
    {
        public static readonly ValueConverter<Money, string> Instance = new(
            money => Format(money),
            value => Parse(value));

        public static string Format(Money money) => $"{money.Amount.ToString(CultureInfo.InvariantCulture)}|{money.Currency}";

        public static Money Parse(string value)
        {
            var parts = value.Split('|');
            return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
        }
    }

    private static class NullableMoneyConverter
    {
        public static readonly ValueConverter<Money?, string?> Instance = new(
            money => money == null ? null : MoneyConverter.Format(money),
            value => value == null ? null : MoneyConverter.Parse(value));
    }
}
