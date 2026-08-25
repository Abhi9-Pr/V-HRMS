using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Locations;
using Vespera.Application.Features.Rosters;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Attendance.Regularizations;

/// <summary>
/// Recomputes the one <c>AttendanceDay</c> a regularization touched, once it's approved. Domain
/// events reach here asynchronously, via the outbox (see <c>OutboxDispatcherHostedService</c>) —
/// a background dispatch, with no authenticated HTTP request behind it, so there is no ambient
/// <c>ITenantContext</c> (see <c>HttpTenantContext</c>'s own doc comment: <c>HasTenant</c> is
/// always false outside a request). Sending <c>RecomputeAttendanceDayCommand</c> through
/// <c>ISender</c> here would silently no-op — every read inside that handler is tenant-filtered
/// against an empty tenant id. <c>AttendanceDayComputationHostedService</c> (the nightly sweep)
/// hit this exact problem and solved it the same way: read via <see cref="IReadRepositoryAdmin{T}"/>
/// using an explicit tenant id (here, the one carried on <see cref="RegularizationApproved"/>
/// itself) rather than routing through the tenant-scoped MediatR pipeline. Recomputing from the
/// same punch set always yields the same result, so no dedup guard is needed for at-least-once
/// delivery, unlike <c>EmployeeExitedDomainEventHandler</c>'s checklist creation.
/// </summary>
public sealed class RegularizationApprovedDomainEventHandler : INotificationHandler<DomainEventNotification<RegularizationApproved>>
{
    private readonly IReadRepositoryAdmin<AttendanceDay> _attendanceDaysAdmin;
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter;
    private readonly IReadRepositoryAdmin<Employee> _employeesAdmin;
    private readonly IReadRepositoryAdmin<Location> _locationsAdmin;
    private readonly IReadRepositoryAdmin<ShiftRoster> _rostersAdmin;
    private readonly IReadRepositoryAdmin<Shift> _shiftsAdmin;
    private readonly IReadRepositoryAdmin<Holiday> _holidaysAdmin;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITimeZoneConverter _timeZoneConverter;

    public RegularizationApprovedDomainEventHandler(
        IReadRepositoryAdmin<AttendanceDay> attendanceDaysAdmin,
        IWriteRepository<AttendanceDay> attendanceDayWriter,
        IReadRepositoryAdmin<Employee> employeesAdmin,
        IReadRepositoryAdmin<Location> locationsAdmin,
        IReadRepositoryAdmin<ShiftRoster> rostersAdmin,
        IReadRepositoryAdmin<Shift> shiftsAdmin,
        IReadRepositoryAdmin<Holiday> holidaysAdmin,
        IDateTimeProvider dateTimeProvider,
        ITimeZoneConverter timeZoneConverter)
    {
        _attendanceDaysAdmin = attendanceDaysAdmin;
        _attendanceDayWriter = attendanceDayWriter;
        _employeesAdmin = employeesAdmin;
        _locationsAdmin = locationsAdmin;
        _rostersAdmin = rostersAdmin;
        _shiftsAdmin = shiftsAdmin;
        _holidaysAdmin = holidaysAdmin;
        _dateTimeProvider = dateTimeProvider;
        _timeZoneConverter = timeZoneConverter;
    }

    public async Task Handle(DomainEventNotification<RegularizationApproved> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var tenantId = domainEvent.TenantId;

        var day = FirstOrNull(await _attendanceDaysAdmin.ListIgnoringFiltersAsync(
            new AttendanceDayByIdSpecification(tenantId, domainEvent.AttendanceDayId), cancellationToken));
        if (day is null)
        {
            return;
        }

        var employee = FirstOrNull(await _employeesAdmin.ListIgnoringFiltersAsync(
            new EmployeeByIdSpecification(tenantId, domainEvent.EmployeeId), cancellationToken));
        if (employee is null)
        {
            return;
        }

        var location = FirstOrNull(await _locationsAdmin.ListIgnoringFiltersAsync(
            new LocationByIdSpecification(tenantId, employee.LocationId), cancellationToken));
        if (location is null)
        {
            return;
        }

        var rosterRows = await _rostersAdmin.ListIgnoringFiltersAsync(
            new RostersByEmployeesAndStatusSpecification(tenantId, [domainEvent.EmployeeId.Value], ShiftRosterStatus.Published),
            cancellationToken);

        var assignedShiftId = RosterAssignmentResolver.Resolve(rosterRows, domainEvent.EmployeeId, day.Date);
        var assignedShift = assignedShiftId is { } shiftId
            ? FirstOrNull(await _shiftsAdmin.ListIgnoringFiltersAsync(new ShiftByIdSpecification(tenantId, shiftId), cancellationToken))
            : null;

        var holiday = FirstOrNull(await _holidaysAdmin.ListIgnoringFiltersAsync(
            new HolidayByLocationAndDateSpecification(tenantId, employee.LocationId, day.Date), cancellationToken));
        var isHoliday = holiday is not null;

        // Same documented limitation as RecomputeAttendanceDayCommandHandler: RosterAssignmentResolver
        // can't currently distinguish "pattern says off" from "no roster row at all."
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
        day.ApplyComputation(result, _dateTimeProvider.UtcNow, "system");
        _attendanceDayWriter.Update(day);
    }

    private static T? FirstOrNull<T>(IReadOnlyList<T> items) where T : class => items.Count > 0 ? items[0] : null;
}
