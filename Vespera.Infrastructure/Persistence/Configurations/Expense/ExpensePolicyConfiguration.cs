using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations.Expense;

public sealed class ExpensePolicyConfiguration : TenantScopedEntityConfiguration<ExpensePolicy, ExpensePolicyId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ExpensePolicy> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ExpensePolicyId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Category).IsRequired().HasMaxLength(100);
        builder.Property(e => e.MaxAmountPerClaim).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
        builder.Property(e => e.ReceiptRequiredAboveAmount).HasConversion(MoneyConverter.Instance).HasMaxLength(64);

        builder.Property(e => e.ApplicableDesignationId)
            .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new DesignationId(value.Value) : (DesignationId?)null);

        builder.Property(e => e.MaxAmountSeverity).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.ReceiptRequiredSeverity).HasConversion<string>().HasMaxLength(16);
    }

    private static class MoneyConverter
    {
        public static readonly ValueConverter<Money, string> Instance = new(
            money => $"{money.Amount.ToString(CultureInfo.InvariantCulture)}|{money.Currency}",
            value => Parse(value));

        private static Money Parse(string value)
        {
            var parts = value.Split('|');
            return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
        }
    }
}
