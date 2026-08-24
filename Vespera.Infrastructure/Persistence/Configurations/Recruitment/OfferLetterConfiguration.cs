using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations.Recruitment;

/// <summary>OfferLetter is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot, so it's
/// configured directly — mirrors ExpenseClaimConfiguration's shape.</summary>
public sealed class OfferLetterConfiguration : IEntityTypeConfiguration<OfferLetter>
{
    public void Configure(EntityTypeBuilder<OfferLetter> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new OfferLetterId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.CandidateId).HasConversion(id => id.Value, value => new CandidateId(value));
        builder.Property(e => e.ProposedDesignationId).HasConversion(id => id.Value, value => new DesignationId(value));
        builder.Property(e => e.ProposedCtc).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
        builder.Property(e => e.JoiningDate).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }

    // Duplicated per configuration file — see ExpenseClaimConfiguration/PayrollRunConfiguration
    // for the established rationale (owning entity binds Money via constructor, EF can't bind an
    // owned-navigation-typed constructor parameter).
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
