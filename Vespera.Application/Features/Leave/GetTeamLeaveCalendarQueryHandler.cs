using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.OrgChart;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Flags a day where more than this fraction of the department is out at once — an MVP
/// fixed threshold rather than a configurable policy; a later phase could make it per-department.</summary>
public sealed class GetTeamLeaveCalendarQueryHandler : IRequestHandler<GetTeamLeaveCalendarQuery, Result<IReadOnlyList<TeamLeaveCalendarDayDto>>>
{
    private const decimal ConflictThreshold = 0.3m;

    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public GetTeamLeaveCalendarQueryHandler(
        IReadRepository<User> users, IReadRepository<Employee> employees, IReadRepository<LeaveRequest> leaveRequests,
        ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _users = users;
        _employees = employees;
        _leaveRequests = leaveRequests;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TeamLeaveCalendarDayDto>>> Handle(
        GetTeamLeaveCalendarQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        DepartmentId? departmentId = request.DepartmentId is { } explicitId ? new DepartmentId(explicitId) : null;
        if (departmentId is null)
        {
            if (_currentUser.UserId is not { } userIdValue)
            {
                return Result.Failure<IReadOnlyList<TeamLeaveCalendarDayDto>>(
                    Error.Unauthorized("leave_calendar.not_authenticated", "Not authenticated."));
            }

            var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
            if (callerUser?.EmployeeId is not { } callerEmployeeId)
            {
                return Result.Success<IReadOnlyList<TeamLeaveCalendarDayDto>>([]);
            }

            var callerEmployee = await _employees.FirstOrDefaultAsync(
                new EmployeeByIdSpecification(tenantId, callerEmployeeId), cancellationToken);
            if (callerEmployee is null)
            {
                return Result.Success<IReadOnlyList<TeamLeaveCalendarDayDto>>([]);
            }

            departmentId = callerEmployee.DepartmentId;
        }

        var allEmployees = await _employees.ListAsync(new EmployeesByTenantSpecification(tenantId), cancellationToken);
        var teamEmployeeIds = allEmployees.Where(e => e.DepartmentId == departmentId).Select(e => e.Id).ToHashSet();
        if (teamEmployeeIds.Count == 0)
        {
            return Result.Success<IReadOnlyList<TeamLeaveCalendarDayDto>>([]);
        }

        var approvedRequests = await _leaveRequests.ListAsync(
            new ApprovedLeaveRequestsOverlappingRangeSpecification(tenantId), cancellationToken);
        var teamRequests = approvedRequests
            .Where(r => teamEmployeeIds.Contains(r.EmployeeId) && r.Period.Start <= request.To && r.Period.End >= request.From)
            .ToList();

        var days = new List<TeamLeaveCalendarDayDto>();
        for (var date = request.From; date <= request.To; date = date.AddDays(1))
        {
            var onLeave = teamRequests.Where(r => r.Period.Contains(date)).Select(r => r.EmployeeId.Value).Distinct().ToList();
            var conflict = teamEmployeeIds.Count > 0 && (decimal)onLeave.Count / teamEmployeeIds.Count > ConflictThreshold;
            days.Add(new TeamLeaveCalendarDayDto(date, onLeave, conflict));
        }

        return Result.Success<IReadOnlyList<TeamLeaveCalendarDayDto>>(days);
    }
}
