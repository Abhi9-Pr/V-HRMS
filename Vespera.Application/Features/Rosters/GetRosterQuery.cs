using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Rosters;

public sealed record GetRosterQuery(
    DateOnly RangeStart,
    DateOnly RangeEnd,
    Guid? DepartmentId,
    Guid? EmployeeId) : IRequest<Result<RosterResultDto>>;

public sealed record RosterResultDto(IReadOnlyList<EmployeeRosterDto> Employees);

public sealed record EmployeeRosterDto(Guid EmployeeId, string EmployeeName, IReadOnlyList<RosterDayDto> Days);

public sealed record RosterDayDto(DateOnly Date, Guid? ShiftId, string? ShiftName);
