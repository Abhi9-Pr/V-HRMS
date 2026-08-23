using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class BlackoutPeriodConfiguration : TenantScopedEntityConfiguration<BlackoutPeriod, BlackoutPeriodId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BlackoutPeriod> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new BlackoutPeriodId(value))
            .ValueGeneratedNever();

        // Plain scalar conversion — same reasoning as LeaveRequestConfiguration.Period.
        builder.Property(e => e.Period)
            .HasConversion(
                range => $"{range.Start:O}|{range.End:O}",
                value => ParseRange(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Reason).IsRequired().HasMaxLength(500);
        builder.Property(e => e.LeaveTypeId).HasConversion(
            id => id == null ? (Guid?)null : id.Value.Value,
            value => value == null ? (LeaveTypeId?)null : new LeaveTypeId(value.Value));

        builder.HasIndex(e => new { e.TenantId, e.LeaveTypeId });
    }

    private static DateRange ParseRange(string value)
    {
        var parts = value.Split('|');
        var start = DateOnly.ParseExact(parts[0], "O", CultureInfo.InvariantCulture);
        var end = DateOnly.ParseExact(parts[1], "O", CultureInfo.InvariantCulture);
        return DateRange.Create(start, end).Value;
    }
}
