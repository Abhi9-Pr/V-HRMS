using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Application.Features.Departments;
using Vespera.Application.Features.Designations;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Holidays;
using Vespera.Application.Features.Leave;
using Vespera.Application.Features.Locations;
using Vespera.Application.Features.Rosters;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Mobile;

/// <summary>Composes several already-existing self-service queries into one round trip, the same
/// composition-over-<c>ISender</c> pattern <c>ShiftTrackerWidgetProvider</c> and the other
/// dashboard widget providers use — no new persistence access beyond the caller's own profile and
/// geofences, everything else is delegated.</summary>
public sealed class GetMobileBootstrapQueryHandler : IRequestHandler<GetMobileBootstrapQuery, Result<MobileBootstrapDto>>
{
    private const int UpcomingShiftsDays = 14;

    private readonly ISender _sender;
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<GeofenceZone> _geofenceZones;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetMobileBootstrapQueryHandler(
        ISender sender,
        IReadRepository<Employee> employees,
        IReadRepository<GeofenceZone> geofenceZones,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _sender = sender;
        _employees = employees;
        _geofenceZones = geofenceZones;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<MobileBootstrapDto>> Handle(GetMobileBootstrapQuery request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure<MobileBootstrapDto>(
                Error.Failure("mobile_bootstrap.no_linked_employee", "This account isn't linked to an employee record."));
        }

        var tenantId = _tenantContext.TenantId;

        var employee = await _employees.FirstOrDefaultAsync(
            new Employees.EmployeeByIdSpecification(tenantId, employeeId.Value), cancellationToken);
        if (employee is null)
        {
            return Result.Failure<MobileBootstrapDto>(Error.NotFound("mobile_bootstrap.employee_not_found", "Employee record not found."));
        }

        var profile = new EmployeeProfileDto(
            employee.Id.Value, employee.Code.Value, employee.FirstName, employee.LastName, employee.WorkEmail.Value,
            employee.DepartmentId.Value, employee.DesignationId.Value, employee.LocationId.Value, employee.Status.ToString());

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var rosterResult = await _sender.Send(
            new GetRosterQuery(today, today.AddDays(UpcomingShiftsDays - 1), null, employeeId.Value.Value), cancellationToken);
        var upcomingShifts = rosterResult.IsSuccess && rosterResult.Value.Employees.Count > 0
            ? rosterResult.Value.Employees[0].Days
            : [];

        var geofenceZones = await _geofenceZones.ListAsync(
            new GeofenceZoneByLocationIdSpecification(tenantId, employee.LocationId), cancellationToken);
        var geofences = geofenceZones
            .Select(zone => new GeofenceZoneDto(zone.Id.Value, zone.Name, zone.Center.Latitude, zone.Center.Longitude, zone.RadiusMetres))
            .ToList();

        var policyVersions = new PolicyVersionsDto(
            await GetETagAsync(new GetDepartmentsETagQuery(), cancellationToken),
            await GetETagAsync(new GetDesignationsETagQuery(), cancellationToken),
            await GetETagAsync(new GetLocationsETagQuery(), cancellationToken),
            await GetETagAsync(new GetLeaveTypesETagQuery(), cancellationToken),
            await GetETagAsync(new GetHolidaysETagQuery(LocationId: null), cancellationToken));

        return Result.Success(new MobileBootstrapDto(profile, upcomingShifts, geofences, policyVersions));
    }

    private async Task<string> GetETagAsync(IRequest<Result<string>> query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? result.Value : string.Empty;
    }
}
