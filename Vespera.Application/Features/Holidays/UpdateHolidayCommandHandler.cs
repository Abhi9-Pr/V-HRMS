using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed class UpdateHolidayCommandHandler : IRequestHandler<UpdateHolidayCommand, Result>
{
    private readonly IReadRepository<Holiday> _holidays;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateHolidayCommandHandler(
        IReadRepository<Holiday> holidays,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _holidays = holidays;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateHolidayCommand request, CancellationToken cancellationToken)
    {
        var specification = new HolidayByIdSpecification(_tenantContext.TenantId, new HolidayId(request.Id));

        var holiday = await _holidays.FirstOrDefaultAsync(specification, cancellationToken);
        if (holiday is null)
        {
            return Result.Failure(Error.NotFound("holiday.not_found", "Holiday not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        var renameResult = holiday.Rename(request.Name, now, modifiedBy);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        return holiday.Reschedule(request.Date, now, modifiedBy);
    }
}
