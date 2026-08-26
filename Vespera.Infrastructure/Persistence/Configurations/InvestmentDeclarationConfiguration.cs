using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class InvestmentDeclarationConfiguration : IEntityTypeConfiguration<InvestmentDeclaration>
{
    public void Configure(EntityTypeBuilder<InvestmentDeclaration> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new InvestmentDeclarationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.TaxRegimeVersionId).HasConversion(id => id.Value, value => new TaxRegimeVersionId(value));
        builder.Property(e => e.FinancialYear).IsRequired().HasMaxLength(16);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.FinancialYear }).IsUnique();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.OwnsMany(e => e.Lines, lines =>
        {
            lines.ToTable("InvestmentDeclarationLines");
            lines.Property(l => l.Section).IsRequired().HasMaxLength(64);
            lines.Property(l => l.Amount)
                .HasConversion(amount => $"{amount.Amount.ToString(CultureInfo.InvariantCulture)}|{amount.Currency}", value => ParseMoney(value))
                .HasMaxLength(64);
            lines.Property(l => l.ProofFileReference).HasMaxLength(500);
            lines.Property(l => l.ReviewStatus).HasConversion<string>().HasMaxLength(16).IsRequired();
            lines.Property(l => l.ReviewComment).HasMaxLength(1000);
            lines.Property(l => l.ReviewedBy).HasMaxLength(256);
            lines.Property(l => l.ReviewedAt);
        });
    }

    private static Money ParseMoney(string value)
    {
        var parts = value.Split('|');
        return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
    }
}
