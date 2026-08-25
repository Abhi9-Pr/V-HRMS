using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class UpdateShiftCommandHandler : IRequestHandler<UpdateShiftCommand, Result>
{
    private readonly IReadRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateShiftCommandHandler(
        IReadRepository<Shift> shifts,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _shifts = shifts;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateShiftCommand request, CancellationToken cancellationToken)
    {
        var specification = new ShiftByIdSpecification(_tenantContext.TenantId, new ShiftId(request.Id));

        var shift = await _shifts.FirstOrDefaultAsync(specification, cancellationToken);
        if (shift is null)
        {
            return Result.Failure(Error.NotFound("shift.not_found", "Shift not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        var renameResult = shift.Rename(request.Name, now, modifiedBy);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        var rescheduleResult = shift.Reschedule(request.StartTime, request.EndTime, now, modifiedBy);
        if (rescheduleResult.IsFailure)
        {
            return rescheduleResult;
        }

        return shift.ConfigureBreak(request.BreakMinutes, now, modifiedBy);
    }
}
