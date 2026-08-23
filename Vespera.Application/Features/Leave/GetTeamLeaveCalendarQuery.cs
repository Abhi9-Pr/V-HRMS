using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary><paramref name="DepartmentId"/> null defaults to the caller's own department.</summary>
public sealed record GetTeamLeaveCalendarQuery(DateOnly From, DateOnly To, Guid? DepartmentId) : IRequest<Result<IReadOnlyList<TeamLeaveCalendarDayDto>>>;

public sealed record TeamLeaveCalendarDayDto(DateOnly Date, IReadOnlyList<Guid> EmployeeIdsOnLeave, bool ConflictWarning);
