using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Rosters;

public sealed class OverrideRosterAssignmentCommandHandler : IRequestHandler<OverrideRosterAssignmentCommand, Result<Guid>>
{
    private readonly IReadRepository<Shift> _shifts;
    private readonly IWriteRepository<ShiftRoster> _rosters;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OverrideRosterAssignmentCommandHandler(
        IReadRepository<Shift> shifts,
        IWriteRepository<ShiftRoster> rosters,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _shifts = shifts;
        _rosters = rosters;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(OverrideRosterAssignmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var shiftId = new ShiftId(request.ShiftId);

        var shiftExists = await _shifts.AnyAsync(new ShiftByIdSpecification(tenantId, shiftId), cancellationToken);
        if (!shiftExists)
        {
            return Result.Failure<Guid>(Error.NotFound("shift.not_found", "Shift not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var actor = _currentUser.UserId?.ToString() ?? "system";
        var period = DateRange.Create(request.Date, request.Date).Value;

        var roster = ShiftRoster.Create(tenantId, new EmployeeId(request.EmployeeId), shiftId, period, now, isOverride: true);
        var publishResult = roster.Publish(now, actor);
        if (publishResult.IsFailure)
        {
            return Result.Failure<Guid>(publishResult.Error);
        }

        await _rosters.AddAsync(roster, cancellationToken);

        return Result.Success(roster.Id.Value);
    }
}
