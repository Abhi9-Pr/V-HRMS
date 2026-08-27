using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed record ShiftTrackerWidgetDto(
    Guid? EmployeeId, string? ShiftName, TimeOnly? ShiftStart, TimeOnly? ShiftEnd, string PunchStatus, DateTimeOffset? FirstIn,
    DateTimeOffset? LastOut, int WorkedMinutes);

/// <summary>Today's assigned shift plus current punch state — "today" is resolved against UTC
/// (not the employee's work-location timezone, unlike <c>RecordWebPunchCommandHandler</c>) as a
/// deliberate simplification for a display-only widget; the actual punch commands remain the
/// source of truth for timezone-correct day boundaries. Quick-punch itself reuses the existing
/// <c>RecordWebPunchCommand</c>/<c>RecordMobilePunchCommand</c> endpoints — this provider is
/// read-only.</summary>
public sealed class ShiftTrackerWidgetProvider : IDashboardWidgetProvider
{
    private readonly ISender _sender;
    private readonly IReadRepository<ShiftRoster> _shiftRosters;
    private readonly IReadRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public ShiftTrackerWidgetProvider(
        ISender sender, IReadRepository<ShiftRoster> shiftRosters, IReadRepository<Shift> shifts, ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _sender = sender;
        _shiftRosters = shiftRosters;
        _shifts = shifts;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public string WidgetKey => "shiftTracker";

    public int DefaultOrder => 0;

    public bool DefaultVisible => true;

    public WidgetSize DefaultSize => WidgetSize.Medium;

    public async Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Success<object?>(new ShiftTrackerWidgetDto(null, null, null, null, "Unavailable", null, null, 0));
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);

        var rosterRows = await _shiftRosters.ListAsync(
            new ShiftRostersByEmployeeSpecification(_tenantContext.TenantId, employeeId.Value), cancellationToken);
        var shiftId = RosterAssignmentResolver.Resolve(rosterRows, employeeId.Value, today);

        string? shiftName = null;
        TimeOnly? shiftStart = null;
        TimeOnly? shiftEnd = null;
        if (shiftId is { } resolvedShiftId)
        {
            var shift = await _shifts.FirstOrDefaultAsync(new ShiftByIdSpecification(_tenantContext.TenantId, resolvedShiftId), cancellationToken);
            if (shift is not null)
            {
                shiftName = shift.Name;
                shiftStart = shift.StartTime;
                shiftEnd = shift.EndTime;
            }
        }

        var dayResult = await _sender.Send(new GetAttendanceDayQuery(employeeId.Value.Value, today), cancellationToken);
        if (dayResult.IsFailure)
        {
            return Result.Success<object?>(new ShiftTrackerWidgetDto(employeeId.Value.Value, shiftName, shiftStart, shiftEnd, "NotStarted", null, null, 0));
        }

        var day = dayResult.Value;
        var punchStatus = day.FirstIn is null ? "NotStarted" : day.LastOut is null ? "In" : "Out";

        return Result.Success<object?>(new ShiftTrackerWidgetDto(
            employeeId.Value.Value, shiftName, shiftStart, shiftEnd, punchStatus, day.FirstIn, day.LastOut, day.WorkedMinutes));
    }
}
