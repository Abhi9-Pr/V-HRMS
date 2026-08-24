using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class ProxyDelegationConfiguration : IEntityTypeConfiguration<ProxyDelegation>
{
    public void Configure(EntityTypeBuilder<ProxyDelegation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ProxyDelegationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.DelegatorId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.DelegateId).HasConversion(id => id.Value, value => new EmployeeId(value));

        // Plain scalar conversion, not OwnsOne — ProxyDelegation's constructor binds DateRange
        // directly (see PayrollRunConfiguration/ExpenseClaimConfiguration for the same pattern
        // with Money: EF can't bind an owned-navigation-typed constructor parameter).
        builder.Property(e => e.Validity).HasConversion(DateRangeConverter.Instance).HasMaxLength(32);

        builder.Property(e => e.Scope).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.IsRevoked).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }

    private static class DateRangeConverter
    {
        public static readonly ValueConverter<DateRange, string> Instance = new(
            range => $"{range.Start:O}|{range.End:O}",
            value => Parse(value));

        private static DateRange Parse(string value)
        {
            var parts = value.Split('|');
            return DateRange.Create(DateOnly.Parse(parts[0]), DateOnly.Parse(parts[1])).Value;
        }
    }
}
