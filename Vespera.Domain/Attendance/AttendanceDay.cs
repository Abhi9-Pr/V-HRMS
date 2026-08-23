using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Attendance;

public readonly record struct AttendanceDayId(Guid Value)
{
    public static AttendanceDayId New() => new(Guid.NewGuid());
}

public enum AttendanceDayStatus
{
    Present,
    Absent,
    HalfDay,
    OnLeave,
    Holiday,
    WeekOff,
}

public sealed class AttendanceDay : AggregateRoot<AttendanceDayId>, ITenantScoped, ISoftDeletable
{
    private readonly List<AttendancePunch> _punches = [];

    private AttendanceDay(AttendanceDayId id, TenantId tenantId, EmployeeId employeeId, DateOnly date)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Date = date;
        Status = AttendanceDayStatus.Absent;
        LastChangedAt = DateTimeOffset.MinValue;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public DateOnly Date { get; }

    public AttendanceDayStatus Status { get; private set; }

    public DateTimeOffset? FirstIn { get; private set; }

    public DateTimeOffset? LastOut { get; private set; }

    public int WorkedMinutes { get; private set; }

    public int LateByMinutes { get; private set; }

    public int EarlyLeaveByMinutes { get; private set; }

    public int OvertimeMinutes { get; private set; }

    public bool IsLopCandidate { get; private set; }

    public DateTimeOffset? LastComputedAt { get; private set; }

    public string? LastComputedBy { get; private set; }

    /// <summary>The instant this day's own state last changed (a punch recorded, a recompute
    /// applied) — the cursor field mobile delta-sync (<c>GetMyAttendanceDeltaSyncQuery</c>) orders
    /// and pages on. Not a <c>ModifiedAt</c>-style audit field (this type isn't auditable) — a
    /// purpose-built field for this one consumer.</summary>
    public DateTimeOffset LastChangedAt { get; private set; }

    /// <summary>An <see cref="AttendanceDay"/> is a computed historical record, never actually
    /// deleted — always <see langword="false"/>, and nothing on this type ever sets it otherwise.
    /// Implemented (as a real mapped column, not a computed expression) only so this type can be a
    /// <see cref="DeltaSyncQueryHandlerBase{TRequest,TEntity,TDto}"/> <c>TEntity</c> — the shared
    /// soft-delete <c>HasQueryFilter</c> convention in <c>VesperaDbContext</c> applies to every
    /// <c>ISoftDeletable</c> type and needs a real, translatable property to filter on, not a
    /// C#-only constant. The tombstone path that constraint exists for is structurally present but
    /// currently unreachable for this type.</summary>
    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string? DeletedBy { get; private set; }

    public IReadOnlyCollection<AttendancePunch> Punches => _punches.AsReadOnly();

    public TimeSpan WorkedTime
    {
        get
        {
            var total = TimeSpan.Zero;
            DateTimeOffset? inAt = null;

            foreach (var punch in _punches)
            {
                if (punch.PunchType == PunchType.In)
                {
                    inAt = punch.PunchedAtUtc;
                }
                else if (inAt is { } start)
                {
                    total += punch.PunchedAtUtc - start;
                    inAt = null;
                }
            }

            return total;
        }
    }

    public static AttendanceDay Open(TenantId tenantId, EmployeeId employeeId, DateOnly date) =>
        new(AttendanceDayId.New(), tenantId, employeeId, date);

    public Result RecordPunch(
        PunchType punchType,
        DateTimeOffset punchedAtUtc,
        GeoCoordinate? location,
        PunchSource source,
        bool requiresApproval = false,
        string? flagReason = null)
    {
        if (_punches.Count > 0)
        {
            var last = _punches[^1];

            if (last.PunchType == punchType)
            {
                return Result.Failure(Error.Conflict(
                    "attendance_day.consecutive_same_punch", $"Cannot record two consecutive {punchType} punches."));
            }

            if (punchedAtUtc < last.PunchedAtUtc)
            {
                return Result.Failure(Error.Validation(
                    "attendance_day.out_of_order_punch", "Punch time cannot be before the previous punch."));
            }
        }
        else if (punchType != PunchType.In)
        {
            return Result.Failure(Error.Validation(
                "attendance_day.must_punch_in_first", "The first punch of the day must be an In punch."));
        }

        _punches.Add(new AttendancePunch(AttendancePunchId.New(), punchType, punchedAtUtc, location, source, requiresApproval, flagReason));
        Status = AttendanceDayStatus.Present;
        LastChangedAt = punchedAtUtc;
        Raise(new PunchRecorded(EmployeeId, punchedAtUtc, punchType, punchedAtUtc));
        return Result.Success();
    }

    /// <summary>A manager clearing a flagged punch after review — a plain state flip, not a
    /// second approval workflow (a flag is a soft signal, not a pending decision that needs its
    /// own chain).</summary>
    public Result ClearPunchFlag(AttendancePunchId punchId)
    {
        var punch = _punches.FirstOrDefault(p => p.Id == punchId);
        if (punch is null)
        {
            return Result.Failure(Error.NotFound("attendance_punch.not_found", "No such punch on this attendance day."));
        }

        punch.ClearFlag();
        return Result.Success();
    }

    /// <summary>Stores a freshly computed <see cref="AttendanceComputationResult"/> (from
    /// <see cref="AttendanceDayCalculator"/>) onto this day. Idempotent by construction — recomputing
    /// from the same punch set always yields the same result, so calling this twice just overwrites
    /// with the same values; no guard is needed.</summary>
    public Result ApplyComputation(AttendanceComputationResult result, DateTimeOffset occurredOn, string modifiedBy)
    {
        FirstIn = result.FirstIn;
        LastOut = result.LastOut;
        WorkedMinutes = result.WorkedMinutes;
        LateByMinutes = result.LateByMinutes;
        EarlyLeaveByMinutes = result.EarlyLeaveByMinutes;
        OvertimeMinutes = result.OvertimeMinutes;
        IsLopCandidate = result.IsLopCandidate;
        Status = result.Status;
        LastComputedAt = occurredOn;
        LastComputedBy = modifiedBy;
        LastChangedAt = occurredOn;
        return Result.Success();
    }

    /// <summary>Whether this day's last punch is an unmatched <see cref="PunchType.In"/> — i.e. the
    /// employee is still "clocked in" as of the last recorded punch. Used at ingestion time to
    /// decide whether a new punch belongs to *this* (still-open, possibly from a prior calendar
    /// day) <see cref="AttendanceDay"/> rather than a freshly opened one for today — the mechanism
    /// behind a night shift's closing punch landing on the same day it started.</summary>
    public bool IsOpen => _punches.Count > 0 && _punches[^1].PunchType == PunchType.In;
}
