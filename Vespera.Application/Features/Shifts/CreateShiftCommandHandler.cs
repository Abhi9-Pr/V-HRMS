using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class CreateShiftCommandHandler : IRequestHandler<CreateShiftCommand, Result<Guid>>
{
    private readonly IWriteRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateShiftCommandHandler(
        IWriteRepository<Shift> shifts,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _shifts = shifts;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateShiftCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";

        var result = Shift.Create(
            _tenantContext.TenantId, request.Name, request.StartTime, request.EndTime, request.GraceMinutes, now, createdBy);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var shift = result.Value;

        var breakResult = shift.ConfigureBreak(request.BreakMinutes, now, createdBy);
        if (breakResult.IsFailure)
        {
            return Result.Failure<Guid>(breakResult.Error);
        }

        await _shifts.AddAsync(shift, cancellationToken);

        return Result.Success(shift.Id.Value);
    }
}
