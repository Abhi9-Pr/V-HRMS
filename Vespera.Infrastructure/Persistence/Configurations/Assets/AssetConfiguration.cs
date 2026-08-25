using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Assets;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetConfiguration : TenantScopedEntityConfiguration<Asset, AssetId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AssetId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.AssetTag).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => new { e.TenantId, e.AssetTag }).IsUnique();

        builder.Property(e => e.Category).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PurchaseCost).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
        builder.Property(e => e.PurchaseDate).IsRequired();

        builder.Property(e => e.SerialNumber).HasMaxLength(128);
        builder.Property(e => e.MacAddress).HasMaxLength(128);
        builder.Property(e => e.WarrantyExpiryDate);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        // Depreciation is a private-set property (not part of Asset's constructor — it's only
        // ever assigned post-construction via ConfigureDepreciation), so unlike Money/DateRange
        // elsewhere in this codebase it CAN be an OwnsOne: EF doesn't need to bind it into Asset's
        // own constructor. Its own SalvageValue property still can't be a nested OwnsOne though —
        // DepreciationSchedule itself is constructor-bound, so SalvageValue uses the same
        // scalar-conversion trick as everywhere else Money meets a constructor-bound owner.
        builder.OwnsOne(e => e.Depreciation, depreciation =>
        {
            depreciation.Property(d => d.Method).HasConversion<string>().HasMaxLength(32);
            depreciation.Property(d => d.UsefulLifeMonths).IsRequired();
            depreciation.Property(d => d.SalvageValue).HasConversion(MoneyConverter.Instance).HasMaxLength(64);
        });
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
