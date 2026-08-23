using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Leave;

public readonly record struct BlackoutPeriodId(Guid Value)
{
    public static BlackoutPeriodId New() => new(Guid.NewGuid());
}

/// <summary>A window during which leave cannot normally be requested — e.g. a year-end freeze, or a
/// retail team's peak season. <see cref="LeaveTypeId"/> null means it applies to every leave type.</summary>
public sealed class BlackoutPeriod : AuditableTenantAggregateRoot<BlackoutPeriodId>
{
    private BlackoutPeriod(
        BlackoutPeriodId id, TenantId tenantId, DateRange period, string reason, LeaveTypeId? leaveTypeId,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Period = period;
        Reason = reason;
        LeaveTypeId = leaveTypeId;
    }

    public DateRange Period { get; private set; }

    public string Reason { get; private set; }

    public LeaveTypeId? LeaveTypeId { get; private set; }

    public static Result<BlackoutPeriod> Create(
        TenantId tenantId, DateRange period, string reason, LeaveTypeId? leaveTypeId, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<BlackoutPeriod>(Error.Validation("blackout_period.reason_required", "A reason is required."));
        }

        return Result.Success(new BlackoutPeriod(
            BlackoutPeriodId.New(), tenantId, period, reason.Trim(), leaveTypeId, occurredOn, createdBy));
    }

    public bool AppliesTo(LeaveTypeId leaveTypeId) => LeaveTypeId is null || LeaveTypeId == leaveTypeId;

    public bool Overlaps(DateRange other) => Period.Overlaps(other);
}
