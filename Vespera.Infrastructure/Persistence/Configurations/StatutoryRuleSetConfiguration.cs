using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class StatutoryRuleSetConfiguration : TenantScopedReferenceEntityConfiguration<StatutoryRuleSet, StatutoryRuleSetId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StatutoryRuleSet> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new StatutoryRuleSetId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.RuleType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.RatePercent).HasPrecision(7, 4);

        // A plain scalar conversion, not OwnsOne: EF Core can't bind an owned-navigation-typed
        // constructor parameter when materializing the owner via its (required, private)
        // constructor — see LocationConfiguration for the same pattern.
        builder.Property(e => e.CapAmount)
            .HasConversion(
                cap => cap == null ? null : $"{cap.Amount.ToString(CultureInfo.InvariantCulture)}|{cap.Currency}",
                value => ParseMoney(value))
            .HasMaxLength(64);
    }

    private static Money? ParseMoney(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var parts = value.Split('|');
        return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
    }
}
