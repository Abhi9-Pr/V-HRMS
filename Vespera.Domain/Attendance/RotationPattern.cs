using Vespera.Domain.Common;

namespace Vespera.Domain.Attendance;

public readonly record struct RotationPatternId(Guid Value)
{
    public static RotationPatternId New() => new(Guid.NewGuid());
}

/// <summary>One day of a rotation cycle. <see cref="ShiftId"/> is <c>null</c> for an "off" day —
/// this is how a weekly-off falls naturally out of, say, a 7-day pattern, without a separate
/// concept. <see cref="SequenceNumber"/> is 0-based so a caller can resolve "which pattern day
/// applies to this date" as <c>(date - anchor).Days % pattern.Days.Count</c> directly, with no
/// off-by-one translation. No independent lifecycle/identity of its own — the whole list is
/// replaced wholesale on <see cref="RotationPattern.Reconfigure"/>, never edited day-by-day, so a
/// plain immutable record is enough; it doesn't need <see cref="Entity{TId}"/> identity.</summary>
public sealed record RotationPatternDay(int SequenceNumber, ShiftId? ShiftId);

public sealed class RotationPattern : AuditableTenantAggregateRoot<RotationPatternId>
{
    private readonly List<RotationPatternDay> _days;

    private RotationPattern(
        RotationPatternId id, TenantId tenantId, string name, List<RotationPatternDay> days,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        _days = days;
    }

    public string Name { get; private set; }

    public IReadOnlyList<RotationPatternDay> Days => _days;

    public static Result<RotationPattern> Create(
        TenantId tenantId, string name, IReadOnlyList<RotationPatternDay> days,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<RotationPattern>(Error.Validation("rotation_pattern.name_required", "Rotation pattern name is required."));
        }

        var validationError = ValidateDays(days);
        if (validationError is not null)
        {
            return Result.Failure<RotationPattern>(validationError);
        }

        return Result.Success(new RotationPattern(
            RotationPatternId.New(), tenantId, name.Trim(), days.OrderBy(d => d.SequenceNumber).ToList(), occurredOn, createdBy));
    }

    public Result Reconfigure(IReadOnlyList<RotationPatternDay> days, DateTimeOffset occurredOn, string modifiedBy)
    {
        var validationError = ValidateDays(days);
        if (validationError is not null)
        {
            return Result.Failure(validationError);
        }

        _days.Clear();
        _days.AddRange(days.OrderBy(d => d.SequenceNumber));
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    private static Error? ValidateDays(IReadOnlyList<RotationPatternDay> days)
    {
        if (days.Count == 0)
        {
            return Error.Validation("rotation_pattern.no_days", "A rotation pattern must have at least one day.");
        }

        var expectedSequence = Enumerable.Range(0, days.Count);
        var actualSequence = days.Select(d => d.SequenceNumber).OrderBy(s => s);
        if (!expectedSequence.SequenceEqual(actualSequence))
        {
            return Error.Validation(
                "rotation_pattern.invalid_sequence",
                "Sequence numbers must be contiguous, starting at 0, with no gaps or duplicates.");
        }

        return null;
    }
}
