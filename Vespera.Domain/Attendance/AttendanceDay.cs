using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
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

public sealed class AttendanceDay : AggregateRoot<AttendanceDayId>, ITenantScoped
{
    private readonly List<AttendancePunch> _punches = [];

    private AttendanceDay(AttendanceDayId id, TenantId tenantId, EmployeeId employeeId, DateOnly date)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Date = date;
        Status = AttendanceDayStatus.Absent;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public DateOnly Date { get; }

    public AttendanceDayStatus Status { get; private set; }

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

    public Result RecordPunch(PunchType punchType, DateTimeOffset punchedAtUtc, GeoCoordinate? location, PunchSource source)
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

        _punches.Add(new AttendancePunch(AttendancePunchId.New(), punchType, punchedAtUtc, location, source));
        Status = AttendanceDayStatus.Present;
        Raise(new PunchRecorded(EmployeeId, punchedAtUtc, punchType, punchedAtUtc));
        return Result.Success();
    }
}
