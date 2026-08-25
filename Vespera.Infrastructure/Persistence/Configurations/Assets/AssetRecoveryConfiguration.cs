using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetRecoveryConfiguration : IEntityTypeConfiguration<AssetRecovery>
{
    public void Configure(EntityTypeBuilder<AssetRecovery> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AssetRecoveryId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.AssetAssignmentId).HasConversion(id => id.Value, value => new AssetAssignmentId(value));
        builder.Property(e => e.AssetId).HasConversion(id => id.Value, value => new AssetId(value));
        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));

        builder.Property(e => e.InitiatedAt).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(e => e.CourierCarrier).HasMaxLength(128);
        builder.Property(e => e.CourierTrackingReference).HasMaxLength(128);
        builder.Property(e => e.ReceivedAt);
        builder.Property(e => e.DamageAssessmentNotes).HasMaxLength(1024);
        builder.Property(e => e.WriteOffAmount).HasConversion(NullableMoneyConverter.Instance).HasMaxLength(64);
        builder.Property(e => e.WriteOffReason).HasMaxLength(1024);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }

    private static class MoneyConverter
    {
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
