using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

/// <summary>The one employee-attendance read endpoint this module had gone without so far — every
/// prior milestone only added mutating actions (punch, clear-flag, recompute); this is what lets a
/// caller (or an integration test) see the computed state <c>ApplyComputation</c> produced.</summary>
public sealed record GetAttendanceDayQuery(Guid EmployeeId, DateOnly Date) : IRequest<Result<AttendanceDayDto>>;

public sealed record AttendanceDayDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    string Status,
    DateTimeOffset? FirstIn,
    DateTimeOffset? LastOut,
    int WorkedMinutes,
    int LateByMinutes,
    int EarlyLeaveByMinutes,
    int OvertimeMinutes,
    bool IsLopCandidate,
    DateTimeOffset? LastComputedAt,
    string? LastComputedBy);
