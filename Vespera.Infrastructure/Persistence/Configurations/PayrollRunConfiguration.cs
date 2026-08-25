using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class PayrollRunConfiguration : TenantScopedEntityConfiguration<PayrollRun, PayrollRunId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new PayrollRunId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Month).IsRequired();
        builder.Property(e => e.Year).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.OwnsMany(e => e.Lines, lines =>
        {
            lines.ToTable("PayrollLines");
            lines.HasKey(l => l.Id);
            lines.Property(l => l.Id)
                .HasConversion(id => id.Value, value => new PayrollLineId(value))
                .ValueGeneratedNever();

            lines.Property(l => l.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));

            // Plain scalar conversions, not OwnsOne — see LocationConfiguration/StatutoryRuleSetConfiguration
            // for why: PayrollLine's constructor binds Money parameters directly, and EF can't
            // bind an owned-navigation-typed constructor parameter.
            lines.Property(l => l.Gross).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
            lines.Property(l => l.Deductions).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
            lines.Property(l => l.Net).HasConversion(MoneyConverter.Instance).HasMaxLength(64);

            lines.Property(l => l.LossOfPayDays).HasPrecision(5, 2);
        });

        builder.OwnsMany(e => e.Reimbursements, reimbursements =>
        {
            reimbursements.ToTable("PayrollReimbursements");
            reimbursements.HasKey(r => r.Id);
            reimbursements.Property(r => r.Id)
                .HasConversion(id => id.Value, value => new PayrollReimbursementId(value))
                .ValueGeneratedNever();

            reimbursements.Property(r => r.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
            reimbursements.Property(r => r.Amount).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
            reimbursements.Property(r => r.SourceExpenseClaimId).IsRequired();
        });
    }

    private static class MoneyConverter
    {
        public static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Money, string> Instance = new(
            money => $"{money.Amount.ToString(CultureInfo.InvariantCulture)}|{money.Currency}",
            value => Parse(value));

        private static Money Parse(string value)
        {
            var parts = value.Split('|');
            return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
        }
    }
}
