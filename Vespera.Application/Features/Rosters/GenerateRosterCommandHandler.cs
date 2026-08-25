using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.RotationPatterns;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Rosters;

public sealed class GenerateRosterCommandHandler : IRequestHandler<GenerateRosterCommand, Result<int>>
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns;
    private readonly IWriteRepository<ShiftRoster> _rosters;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GenerateRosterCommandHandler(
        IReadRepository<RotationPattern> rotationPatterns,
        IWriteRepository<ShiftRoster> rosters,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _rotationPatterns = rotationPatterns;
        _rosters = rosters;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<int>> Handle(GenerateRosterCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var pattern = await _rotationPatterns.FirstOrDefaultAsync(
            new RotationPatternByIdSpecification(tenantId, new RotationPatternId(request.RotationPatternId)), cancellationToken);
        if (pattern is null)
        {
            return Result.Failure<int>(Error.NotFound("rotation_pattern.not_found", "Rotation pattern not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";
        var cycleLength = pattern.Days.Count;
        var createdCount = 0;

        foreach (var employeeGuid in request.EmployeeIds)
        {
            var employeeId = new EmployeeId(employeeGuid);

            ShiftId? runShiftId = null;
            DateOnly runStart = default;
            DateOnly runEnd = default;

            for (var date = request.RangeStart; date <= request.RangeEnd; date = date.AddDays(1))
            {
                var dayIndex = Modulo(date.DayNumber - request.PatternAnchorDate.DayNumber, cycleLength);
                var dayShiftId = pattern.Days.First(day => day.SequenceNumber == dayIndex).ShiftId;

                if (runShiftId is null)
                {
                    runShiftId = dayShiftId;
                    runStart = date;
                    runEnd = date;
                    continue;
                }

                if (dayShiftId == runShiftId)
                {
                    runEnd = date;
                    continue;
                }

                createdCount += await FlushRunAsync(tenantId, employeeId, runShiftId, runStart, runEnd, now, cancellationToken);

                runShiftId = dayShiftId;
                runStart = date;
                runEnd = date;
            }

            createdCount += await FlushRunAsync(tenantId, employeeId, runShiftId, runStart, runEnd, now, cancellationToken);
        }

        return Result.Success(createdCount);
    }

    private async Task<int> FlushRunAsync(
        TenantId tenantId, EmployeeId employeeId, ShiftId? shiftId, DateOnly start, DateOnly end, DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (shiftId is not { } realShiftId)
        {
            return 0;
        }

        var period = DateRange.Create(start, end).Value;
        var roster = ShiftRoster.Create(tenantId, employeeId, realShiftId, period, createdAt);
        await _rosters.AddAsync(roster, cancellationToken);
        return 1;
    }

    private static int Modulo(int value, int modulus) => ((value % modulus) + modulus) % modulus;
}
