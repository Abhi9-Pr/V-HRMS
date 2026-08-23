using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Attendance;

public readonly record struct ShiftRosterId(Guid Value)
{
    public static ShiftRosterId New() => new(Guid.NewGuid());
}

public enum ShiftRosterStatus
{
    Draft,
    Published,
}

public sealed class ShiftRoster : AggregateRoot<ShiftRosterId>, ITenantScoped
{
    private ShiftRoster(
        ShiftRosterId id, TenantId tenantId, EmployeeId employeeId, ShiftId shiftId, DateRange period,
        bool isOverride, DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        ShiftId = shiftId;
        Period = period;
        IsOverride = isOverride;
        CreatedAt = createdAt;
        Status = ShiftRosterStatus.Draft;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public ShiftId ShiftId { get; private set; }

    public DateRange Period { get; }

    /// <summary>Whether this row is a single-day, immediately-published correction that takes
    /// precedence over a base (generated) roster row for the same date — see
    /// <see cref="RosterAssignmentResolver"/>.</summary>
    public bool IsOverride { get; }

    public ShiftRosterStatus Status { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public string? PublishedBy { get; private set; }

    /// <summary>No <c>Touch</c>/audit-stamp machinery — this is a plain <see cref="AggregateRoot{TId}"/>,
    /// not an <c>AuditableTenantAggregateRoot</c> (see the class-level context in prior milestones'
    /// notes). Kept anyway, set once at construction, purely so
    /// <see cref="RosterAssignmentResolver"/> has a deterministic tie-break for the rare case of two
    /// same-precedence rows covering the same date.</summary>
    public DateTimeOffset CreatedAt { get; }

    public static ShiftRoster Create(
        TenantId tenantId, EmployeeId employeeId, ShiftId shiftId, DateRange period, DateTimeOffset createdAt,
        bool isOverride = false) =>
        new(ShiftRosterId.New(), tenantId, employeeId, shiftId, period, isOverride, createdAt);

    public Result Reassign(ShiftId shiftId)
    {
        if (shiftId == ShiftId)
        {
            return Result.Failure(Error.Conflict("shift_roster.same_shift", "Employee is already on this shift."));
        }

        ShiftId = shiftId;
        return Result.Success();
    }

    public Result Publish(DateTimeOffset occurredOn, string publishedBy)
    {
        if (Status == ShiftRosterStatus.Published)
        {
            return Result.Failure(Error.Conflict("shift_roster.already_published", "This roster entry is already published."));
        }

        Status = ShiftRosterStatus.Published;
        PublishedAt = occurredOn;
        PublishedBy = publishedBy;
        return Result.Success();
    }
}
