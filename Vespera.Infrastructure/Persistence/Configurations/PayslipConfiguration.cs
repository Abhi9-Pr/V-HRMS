using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new PayslipId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.PayrollRunId).HasConversion(id => id.Value, value => new PayrollRunId(value));
        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.HasIndex(e => new { e.TenantId, e.PayrollRunId, e.EmployeeId }).IsUnique();

        builder.Property(e => e.NetPay)
            .HasConversion(net => $"{net.Amount.ToString(CultureInfo.InvariantCulture)}|{net.Currency}", value => ParseMoney(value))
            .HasMaxLength(64);
        builder.Property(e => e.GeneratedAt).IsRequired();
        builder.Property(e => e.IsPublished).IsRequired();
        builder.Property(e => e.StorageKey).HasMaxLength(500);
        builder.Property(e => e.DocumentHash).HasMaxLength(128);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        // A separate table from PayrollRun's own PayrollLines/PayrollComponentLines owned
        // collections — a payslip's line items are its own copy, frozen at generation time.
        builder.OwnsMany(e => e.Lines, lines =>
        {
            lines.ToTable("PayslipComponentLines");
            lines.HasKey(l => l.Id);
            lines.Property(l => l.Id)
                .HasConversion(id => id.Value, value => new PayrollComponentLineId(value))
                .ValueGeneratedNever();

            lines.Property(l => l.ComponentId).HasConversion(id => id.Value, value => new SalaryComponentId(value));
            lines.Property(l => l.ComponentName).IsRequired().HasMaxLength(200);
            lines.Property(l => l.ComponentType).HasConversion<string>().HasMaxLength(32).IsRequired();
            lines.Property(l => l.Direction).HasConversion<string>().HasMaxLength(16).IsRequired();
            lines.Property(l => l.Amount)
                .HasConversion(amount => $"{amount.Amount.ToString(CultureInfo.InvariantCulture)}|{amount.Currency}", value => ParseMoney(value))
                .HasMaxLength(64);
        });
    }

    private static Money ParseMoney(string value)
    {
        var parts = value.Split('|');
        return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
    }
}
