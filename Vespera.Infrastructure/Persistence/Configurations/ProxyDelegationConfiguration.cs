using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ProxyDelegation"/> had no EF wiring at all before the Attendance regularization
/// workflow needed to query it (same situation <c>GeofenceZone</c>/<c>AttendanceDay</c> were in
/// before the M3 punch-capture milestone) — configured directly, same shape as
/// <c>ShiftRosterConfiguration</c>/<c>RegularizationRequestConfiguration</c>.
/// </summary>
public sealed class ProxyDelegationConfiguration : IEntityTypeConfiguration<ProxyDelegation>
{
    public void Configure(EntityTypeBuilder<ProxyDelegation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ProxyDelegationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new Vespera.Domain.Common.TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.DelegatorId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.DelegateId).HasConversion(id => id.Value, value => new EmployeeId(value));

        // A plain scalar conversion, not OwnsOne: EF Core can't bind an owned-navigation-typed
        // constructor parameter when materializing the owner via its (required, private)
        // constructor — same reasoning as ShiftRosterConfiguration.Period.
        builder.Property(e => e.Validity)
            .HasConversion(
                range => $"{range.Start:O}|{range.End:O}",
                value => ParseRange(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Scope).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.IsRevoked).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.TenantId, e.DelegatorId });
    }

    private static DateRange ParseRange(string value)
    {
        var parts = value.Split('|');
        var start = DateOnly.ParseExact(parts[0], "O", CultureInfo.InvariantCulture);
        var end = DateOnly.ParseExact(parts[1], "O", CultureInfo.InvariantCulture);
        return DateRange.Create(start, end).Value;
    }
}
