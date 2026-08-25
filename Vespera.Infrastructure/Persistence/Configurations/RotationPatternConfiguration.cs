using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class RotationPatternConfiguration : TenantScopedEntityConfiguration<RotationPattern, RotationPatternId>
{
    private sealed record PersistedDay(int SequenceNumber, Guid? ShiftId);

    protected override void ConfigureEntity(EntityTypeBuilder<RotationPattern> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new RotationPatternId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);

        // EF's convention scan would otherwise try to model the public Days property (which has
        // no setter) as a navigation to a RotationPatternDay entity type; it's mapped via the
        // private _days field below instead, so the read-only view is explicitly out of scope.
        builder.Ignore(e => e.Days);

        // Days has no setter (replaced wholesale via Reconfigure, never edited in place) and the
        // owner's private constructor binds it as a plain List<RotationPatternDay>, not an owned
        // navigation — same "plain scalar conversion, not OwnsOne/OwnsMany" reasoning as
        // Employee.CurrentAnnualCtc/StatutoryRuleSet.CapAmount, targeting the backing field
        // directly since the public property is read-only.
        builder.Property<List<RotationPatternDay>>("_days")
            .HasConversion(
                days => JsonSerializer.Serialize(
                    days.Select(d => new PersistedDay(d.SequenceNumber, d.ShiftId != null ? d.ShiftId.Value.Value : (Guid?)null)),
                    (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<PersistedDay>>(json, (JsonSerializerOptions?)null)!
                    .Select(pd => new RotationPatternDay(pd.SequenceNumber, pd.ShiftId != null ? new ShiftId(pd.ShiftId.Value) : null))
                    .ToList())
            .HasColumnName("Days")
            .IsRequired()
            .Metadata.SetValueComparer(new ValueComparer<List<RotationPatternDay>>(
                (left, right) => (left ?? new List<RotationPatternDay>()).SequenceEqual(right ?? new List<RotationPatternDay>()),
                days => days.Aggregate(0, (hash, day) => HashCode.Combine(hash, day.SequenceNumber, day.ShiftId)),
                days => days.ToList()));
    }
}
