using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Rosters;

public sealed class GetRosterQueryHandler : IRequestHandler<GetRosterQuery, Result<RosterResultDto>>
{
    private readonly IReadRepository<Domain.Eis.Employee> _employees;
    private readonly IReadRepository<ShiftRoster> _rosters;
    private readonly IReadRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;

    public GetRosterQueryHandler(
        IReadRepository<Domain.Eis.Employee> employees,
        IReadRepository<ShiftRoster> rosters,
        IReadRepository<Shift> shifts,
        ITenantContext tenantContext)
    {
        _employees = employees;
        _rosters = rosters;
        _shifts = shifts;
        _tenantContext = tenantContext;
    }

    public async Task<Result<RosterResultDto>> Handle(GetRosterQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var employees = await _employees.ListAsync(
            new RosterEmployeesSpecification(tenantId, request.DepartmentId, request.EmployeeId), cancellationToken);

        var employeeIds = employees.Select(employee => employee.Id.Value).ToList();
        var range = DateRange.Create(request.RangeStart, request.RangeEnd).Value;

        var rosterRows = employeeIds.Count == 0
            ? []
            : (await _rosters.ListAsync(
                    new RostersByEmployeesAndStatusSpecification(tenantId, employeeIds, ShiftRosterStatus.Published), cancellationToken))
                .Where(roster => roster.Period.Overlaps(range))
                .ToList();

        var shifts = await _shifts.ListAsync(new ShiftsByTenantSpecification(tenantId), cancellationToken);
        var shiftsById = shifts.ToDictionary(shift => shift.Id, shift => shift);

        var employeeDtos = new List<EmployeeRosterDto>();

        foreach (var employee in employees)
        {
            var days = new List<RosterDayDto>();

            for (var date = request.RangeStart; date <= request.RangeEnd; date = date.AddDays(1))
            {
                var resolvedShiftId = RosterAssignmentResolver.Resolve(rosterRows, employee.Id, date);
                var shiftName = resolvedShiftId is { } shiftId && shiftsById.TryGetValue(shiftId, out var shift)
                    ? shift.Name
                    : null;

                days.Add(new RosterDayDto(date, resolvedShiftId?.Value, shiftName));
            }

            employeeDtos.Add(new EmployeeRosterDto(employee.Id.Value, $"{employee.FirstName} {employee.LastName}", days));
        }

        return Result.Success(new RosterResultDto(employeeDtos));
    }
}
