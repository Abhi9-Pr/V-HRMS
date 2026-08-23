using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Attendance;

public sealed class RecordMobilePunchCommandHandler : IRequestHandler<RecordMobilePunchCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<Location> _locations;
    private readonly IReadRepository<GeofenceZone> _geofenceZones;
    private readonly IReadRepository<AttendanceDay> _attendanceDays;
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITimeZoneConverter _timeZoneConverter;
    private readonly IDistanceCalculator _distanceCalculator;

    public RecordMobilePunchCommandHandler(
        IReadRepository<Employee> employees,
        IReadRepository<Location> locations,
        IReadRepository<GeofenceZone> geofenceZones,
        IReadRepository<AttendanceDay> attendanceDays,
        IWriteRepository<AttendanceDay> attendanceDayWriter,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        ITimeZoneConverter timeZoneConverter,
        IDistanceCalculator distanceCalculator)
    {
        _employees = employees;
        _locations = locations;
        _geofenceZones = geofenceZones;
        _attendanceDays = attendanceDays;
        _attendanceDayWriter = attendanceDayWriter;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _timeZoneConverter = timeZoneConverter;
        _distanceCalculator = distanceCalculator;
    }

    public async Task<Result> Handle(RecordMobilePunchCommand request, CancellationToken cancellationToken)
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

        var punchType = Enum.Parse<PunchType>(request.PunchType, ignoreCase: true);
        var now = _dateTimeProvider.UtcNow;
        var today = _timeZoneConverter.ResolveLocalDate(now, location.TimeZoneId);

        GeoCoordinate? coordinate = null;
        if (request.Latitude is { } latitude && request.Longitude is { } longitude)
        {
            var coordinateResult = GeoCoordinate.Create(latitude, longitude);
            if (coordinateResult.IsFailure)
            {
                return Result.Failure(coordinateResult.Error);
            }

            coordinate = coordinateResult.Value;

            var zone = await _geofenceZones.FirstOrDefaultAsync(
                new GeofenceZoneByLocationIdSpecification(tenantId, employee.LocationId), cancellationToken);
            if (zone is not null)
            {
                var evaluator = new GeofenceEvaluator(_distanceCalculator);
                if (!evaluator.IsInside(zone, coordinate))
                {
                    var actualDistance = _distanceCalculator.DistanceMetres(zone.Center, coordinate);
                    return Result.Failure(Error.Validation(
                        "attendance.punch.outside_geofence",
                        $"You are {actualDistance:F0}m from the site; the allowed radius is {zone.RadiusMetres:F0}m."));
                }
            }
        }

        if (request.IsFromMockProvider && request.RejectMockProvider)
        {
            return Result.Failure(Error.Validation(
                "attendance.punch.mock_location_detected",
                "This punch appears to come from a mock/simulated location provider and has been rejected."));
        }

        var (day, isNewDay) = await AttendanceDayResolver.ResolveAsync(_attendanceDays, tenantId, employeeId, today, cancellationToken);

        var flagReasons = new List<string>();
        if (request.IsFromMockProvider)
        {
            // Only reachable when RejectMockProvider is false — a tenant that relaxed the reject
            // policy still wants the punch surfaced for review.
            flagReasons.Add("mock_location_provider");
        }

        var previousPunch = await FindPreviousPunchAsync(tenantId, employeeId, today, day, isNewDay, cancellationToken);
        if (previousPunch is { Location: { } previousLocation } && coordinate is not null)
        {
            var elapsed = now - previousPunch.PunchedAtUtc;
            var distanceMetres = _distanceCalculator.DistanceMetres(previousLocation, coordinate);
            var impliedSpeedKmh = elapsed <= TimeSpan.Zero
                ? double.PositiveInfinity
                : (distanceMetres / 1000d) / elapsed.TotalHours;

            if (impliedSpeedKmh > request.MaxPlausibleSpeedKmh)
            {
                flagReasons.Add("impossible_travel");
            }
        }

        if (request.Accuracy > request.MinAcceptableAccuracyMetres)
        {
            flagReasons.Add("low_gps_accuracy");
        }

        var requiresApproval = flagReasons.Count > 0;
        var flagReason = requiresApproval ? string.Join(",", flagReasons) : null;

        var recordResult = day.RecordPunch(punchType, now, coordinate, PunchSource.Mobile, requiresApproval, flagReason);
        if (recordResult.IsFailure)
        {
            return recordResult;
        }

        if (isNewDay)
        {
            await _attendanceDayWriter.AddAsync(day, cancellationToken);
        }

        return Result.Success();
    }

    private async Task<AttendancePunch?> FindPreviousPunchAsync(
        TenantId tenantId, EmployeeId employeeId, DateOnly today, AttendanceDay resolvedDay, bool isNewDay, CancellationToken cancellationToken)
    {
        // resolvedDay may be today's day or (for a night shift's closing punch) still-open
        // yesterday's day, per AttendanceDayResolver — either way, its last punch (if any) is the
        // correct "previous punch" to compare against, since it hasn't yet had this new one added.
        if (!isNewDay && resolvedDay.Punches.Count > 0)
        {
            return resolvedDay.Punches.OrderByDescending(p => p.PunchedAtUtc).First();
        }

        var priorDays = await _attendanceDays.ListAsync(
            new MostRecentAttendanceDayBeforeDateSpecification(tenantId, employeeId, today), cancellationToken);
        var priorDay = priorDays.Count > 0 ? priorDays[0] : null;
        return priorDay?.Punches.OrderByDescending(p => p.PunchedAtUtc).FirstOrDefault();
    }
}
