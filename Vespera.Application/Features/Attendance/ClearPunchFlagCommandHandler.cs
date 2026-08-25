using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

public sealed class ClearPunchFlagCommandHandler : IRequestHandler<ClearPunchFlagCommand, Result>
{
    private readonly IReadRepository<AttendanceDay> _attendanceDays;
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter;
    private readonly ITenantContext _tenantContext;

    public ClearPunchFlagCommandHandler(
        IReadRepository<AttendanceDay> attendanceDays,
        IWriteRepository<AttendanceDay> attendanceDayWriter,
        ITenantContext tenantContext)
    {
        _attendanceDays = attendanceDays;
        _attendanceDayWriter = attendanceDayWriter;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(ClearPunchFlagCommand request, CancellationToken cancellationToken)
    {
        var day = await _attendanceDays.FirstOrDefaultAsync(
            new AttendanceDayByEmployeeAndDateSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId), request.Date),
            cancellationToken);
        if (day is null)
        {
            return Result.Failure(Error.NotFound("attendance_day.not_found", "No attendance day for that employee on that date."));
        }

        var result = day.ClearPunchFlag(new AttendancePunchId(request.PunchId));
        if (result.IsFailure)
        {
            return result;
        }

        _attendanceDayWriter.Update(day);
        return Result.Success();
    }
}
