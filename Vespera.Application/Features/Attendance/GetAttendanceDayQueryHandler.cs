using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

public sealed class GetAttendanceDayQueryHandler : IRequestHandler<GetAttendanceDayQuery, Result<AttendanceDayDto>>
{
    private readonly IReadRepository<Domain.Attendance.AttendanceDay> _attendanceDays;
    private readonly ITenantContext _tenantContext;

    public GetAttendanceDayQueryHandler(IReadRepository<Domain.Attendance.AttendanceDay> attendanceDays, ITenantContext tenantContext)
    {
        _attendanceDays = attendanceDays;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AttendanceDayDto>> Handle(GetAttendanceDayQuery request, CancellationToken cancellationToken)
    {
        var day = await _attendanceDays.FirstOrDefaultAsync(
            new AttendanceDayByEmployeeAndDateSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId), request.Date),
            cancellationToken);
        if (day is null)
        {
            return Result.Failure<AttendanceDayDto>(Error.NotFound("attendance_day.not_found", "No attendance day exists for that employee and date."));
        }

        var dto = new AttendanceDayDto(
            day.Id.Value, day.EmployeeId.Value, day.Date, day.Status.ToString(), day.FirstIn, day.LastOut,
            day.WorkedMinutes, day.LateByMinutes, day.EarlyLeaveByMinutes, day.OvertimeMinutes, day.IsLopCandidate,
            day.LastComputedAt, day.LastComputedBy);
        return Result.Success(dto);
    }
}
