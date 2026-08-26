using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="LeaveRequest"/> is a plain <c>AggregateRoot&lt;TId&gt;</c> + <c>IAuditable</c> +
/// <c>ITenantScoped</c> (no soft-delete) — configured directly, same shape as
/// <see cref="RegularizationRequestConfiguration"/>.
/// </summary>
public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LeaveRequestId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.LeaveTypeId).HasConversion(id => id.Value, value => new LeaveTypeId(value));

        // Plain scalar conversion, not OwnsOne — same reasoning as ShiftRosterConfiguration.Period /
        // ProxyDelegationConfiguration.Validity: EF can't bind an owned-navigation-typed constructor
        // parameter when materializing via LeaveRequest's required private constructor.
        builder.Property(e => e.Period)
            .HasConversion(
                range => $"{range.Start:O}|{range.End:O}",
                value => ParseRange(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.RequestedDays).HasPrecision(9, 2);
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.LossOfPayDays).HasPrecision(9, 2);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ModifiedAt);
        builder.Property(e => e.ModifiedBy).HasMaxLength(256);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }

    private static DateRange ParseRange(string value)
    {
        var parts = value.Split('|');
        var start = DateOnly.ParseExact(parts[0], "O", System.Globalization.CultureInfo.InvariantCulture);
        var end = DateOnly.ParseExact(parts[1], "O", System.Globalization.CultureInfo.InvariantCulture);
        return DateRange.Create(start, end).Value;
    }
}
