using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class DeleteShiftCommandHandler : IRequestHandler<DeleteShiftCommand, Result>
{
    private readonly IReadRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteShiftCommandHandler(
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

    public async Task<Result> Handle(DeleteShiftCommand request, CancellationToken cancellationToken)
    {
        var specification = new ShiftByIdSpecification(_tenantContext.TenantId, new ShiftId(request.Id));

        var shift = await _shifts.FirstOrDefaultAsync(specification, cancellationToken);
        if (shift is null)
        {
            return Result.Failure(Error.NotFound("shift.not_found", "Shift not found."));
        }

        return shift.Delete(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
