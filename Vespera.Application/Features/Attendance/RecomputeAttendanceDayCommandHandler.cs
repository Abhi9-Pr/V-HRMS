using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Locations;
using Vespera.Application.Features.Rosters;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Attendance;

public sealed class RecomputeAttendanceDayCommandHandler : IRequestHandler<RecomputeAttendanceDayCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<Location> _locations;
    private readonly IReadRepository<AttendanceDay> _attendanceDays;
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter;
    private readonly IReadRepository<ShiftRoster> _rosters;
    private readonly IReadRepository<Shift> _shifts;
    private readonly IReadRepository<Holiday> _holidays;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITimeZoneConverter _timeZoneConverter;

    public RecomputeAttendanceDayCommandHandler(
        IReadRepository<Employee> employees,
        IReadRepository<Location> locations,
        IReadRepository<AttendanceDay> attendanceDays,
        IWriteRepository<AttendanceDay> attendanceDayWriter,
        IReadRepository<ShiftRoster> rosters,
        IReadRepository<Shift> shifts,
        IReadRepository<Holiday> holidays,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        ITimeZoneConverter timeZoneConverter)
    {
        _employees = employees;
        _locations = locations;
        _attendanceDays = attendanceDays;
        _attendanceDayWriter = attendanceDayWriter;
        _rosters = rosters;
        _shifts = shifts;
        _holidays = holidays;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _timeZoneConverter = timeZoneConverter;
    }

    public async Task<Result> Handle(RecomputeAttendanceDayCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var employeeId = new EmployeeId(request.EmployeeId);

        var employee = await _employees.FirstOrDefaultAsync(new EmployeeByIdSpecification(tenantId, employeeId), cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var location = await _locations.FirstOrDefaultAsync(new LocationByIdSpecification(tenantId, employee.LocationId), cancellationToken);
        if (location is null)
        {
            return Result.Failure(Error.NotFound("location.not_found", "The employee's work location was not found."));
        }

        var rosterRows = await _rosters.ListAsync(
            new RostersByEmployeesAndStatusSpecification(tenantId, [employeeId.Value], ShiftRosterStatus.Published), cancellationToken);

        var shiftsById = new Dictionary<ShiftId, Shift>();
        var now = _dateTimeProvider.UtcNow;

        for (var date = request.RangeStart; date <= request.RangeEnd; date = date.AddDays(1))
        {
            var day = await _attendanceDays.FirstOrDefaultAsync(
                new AttendanceDayByEmployeeAndDateSpecification(tenantId, employeeId, date), cancellationToken);
            var isNewDay = day is null;
            day ??= AttendanceDay.Open(tenantId, employeeId, date);

            var assignedShiftId = RosterAssignmentResolver.Resolve(rosterRows, employeeId, date);
            Shift? assignedShift = null;
            if (assignedShiftId is { } shiftId)
            {
                if (!shiftsById.TryGetValue(shiftId, out assignedShift))
                {
                    assignedShift = await _shifts.FirstOrDefaultAsync(new ShiftByIdSpecification(tenantId, shiftId), cancellationToken);
                    if (assignedShift is not null)
                    {
                        shiftsById[shiftId] = assignedShift;
                    }
                }
            }

            var holiday = await _holidays.FirstOrDefaultAsync(
                new HolidayByLocationAndDateSpecification(tenantId, employee.LocationId, date), cancellationToken);
            var isHoliday = holiday is not null;

            // RosterAssignmentResolver returns only a ShiftId? — it cannot currently distinguish
            // "this date falls within a published roster's rotation pattern, and the pattern day
            // says off" (a real week-off) from "no roster row covers this date at all" (unrostered).
            // Both resolve to assignedShift = null here; isWeekOff is always false as a documented
            // limitation rather than a guess, per the plan's own allowance for this milestone.
            const bool isWeekOff = false;

            var punches = day.Punches.ToList();
            TimeOnly? firstInLocal = null;
            TimeOnly? lastOutLocal = null;
            var firstInUtc = punches.Where(p => p.PunchType == PunchType.In)
                .OrderBy(p => p.PunchedAtUtc).Select(p => (DateTimeOffset?)p.PunchedAtUtc).FirstOrDefault();
            var lastOutUtc = punches.Where(p => p.PunchType == PunchType.Out)
                .OrderByDescending(p => p.PunchedAtUtc).Select(p => (DateTimeOffset?)p.PunchedAtUtc).FirstOrDefault();

            if (firstInUtc is { } inUtc)
            {
                var zoned = _timeZoneConverter.ToZoned(inUtc, location.TimeZoneId);
                firstInLocal = new TimeOnly(zoned.Hour, zoned.Minute, zoned.Second);
            }

            if (lastOutUtc is { } outUtc)
            {
                var zoned = _timeZoneConverter.ToZoned(outUtc, location.TimeZoneId);
                lastOutLocal = new TimeOnly(zoned.Hour, zoned.Minute, zoned.Second);
            }

            var result = AttendanceDayCalculator.Compute(punches, assignedShift, isHoliday, isWeekOff, firstInLocal, lastOutLocal);
            day.ApplyComputation(result, now, "system");

            if (isNewDay)
            {
                await _attendanceDayWriter.AddAsync(day, cancellationToken);
            }
            else
            {
                _attendanceDayWriter.Update(day);
            }
        }

        return Result.Success();
    }
}
