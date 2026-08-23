using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

/// <summary>
/// Runs inside an authenticated request, so — unlike <see cref="BackgroundJobs.BiometricPunchPollerHostedService"/>
/// (no ambient <c>ITenantContext</c>) — this can use the real <see cref="AttendanceDayResolver"/>
/// against the tenant-scoped <see cref="IReadRepository{T}"/>, the same way the web/mobile punch
/// handlers do.
/// </summary>
public sealed class ResolveQuarantinedPunchCommandHandler : IRequestHandler<ResolveQuarantinedPunchCommand, Result>
{
    private readonly IReadRepository<QuarantinedBiometricPunch> _quarantinedPunches;
    private readonly IWriteRepository<QuarantinedBiometricPunch> _quarantinedPunchWriter;
    private readonly IReadRepository<Employee> _employees;
    private readonly IWriteRepository<Employee> _employeeWriter;
    private readonly IReadRepository<Location> _locations;
    private readonly IReadRepository<AttendanceDay> _attendanceDays;
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITimeZoneConverter _timeZoneConverter;
    private readonly ICurrentUser _currentUser;

    public ResolveQuarantinedPunchCommandHandler(
        IReadRepository<QuarantinedBiometricPunch> quarantinedPunches,
        IWriteRepository<QuarantinedBiometricPunch> quarantinedPunchWriter,
        IReadRepository<Employee> employees,
        IWriteRepository<Employee> employeeWriter,
        IReadRepository<Location> locations,
        IReadRepository<AttendanceDay> attendanceDays,
        IWriteRepository<AttendanceDay> attendanceDayWriter,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        ITimeZoneConverter timeZoneConverter,
        ICurrentUser currentUser)
    {
        _quarantinedPunches = quarantinedPunches;
        _quarantinedPunchWriter = quarantinedPunchWriter;
        _employees = employees;
        _employeeWriter = employeeWriter;
        _locations = locations;
        _attendanceDays = attendanceDays;
        _attendanceDayWriter = attendanceDayWriter;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _timeZoneConverter = timeZoneConverter;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ResolveQuarantinedPunchCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var entry = await _quarantinedPunches.FirstOrDefaultAsync(
            new QuarantinedBiometricPunchByIdSpecification(tenantId, new QuarantinedBiometricPunchId(request.QuarantinedBiometricPunchId)),
            cancellationToken);
        if (entry is null)
        {
            return Result.Failure(Error.NotFound("quarantined_biometric_punch.not_found", "Quarantined punch not found."));
        }

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

        var resolveResult = entry.Resolve(employeeId);
        if (resolveResult.IsFailure)
        {
            return resolveResult;
        }

        var today = _timeZoneConverter.ResolveLocalDate(entry.PunchedAtUtc, location.TimeZoneId);
        var (day, isNewDay) = await AttendanceDayResolver.ResolveAsync(_attendanceDays, tenantId, employeeId, today, cancellationToken);

        var punchType = entry.PunchType ?? (day.IsOpen ? PunchType.Out : PunchType.In);
        var recordResult = day.RecordPunch(punchType, entry.PunchedAtUtc, null, PunchSource.Biometric);
        if (recordResult.IsFailure)
        {
            return recordResult;
        }

        if (isNewDay)
        {
            await _attendanceDayWriter.AddAsync(day, cancellationToken);
        }
        else
        {
            _attendanceDayWriter.Update(day);
        }

        // So future punches from this same device user id resolve automatically instead of
        // landing in quarantine again — only when the employee has no mapping yet, never
        // overwriting one already set.
        if (employee.BiometricDeviceUserId is null)
        {
            var now = _dateTimeProvider.UtcNow;
            var modifiedBy = _currentUser.UserId?.ToString() ?? "system";
            employee.AssignBiometricDeviceUserId(entry.DeviceUserId, now, modifiedBy);
            _employeeWriter.Update(employee);
        }

        _quarantinedPunchWriter.Update(entry);

        return Result.Success();
    }
}
