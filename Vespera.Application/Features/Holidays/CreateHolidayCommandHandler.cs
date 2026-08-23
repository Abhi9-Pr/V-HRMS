using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Holidays;

public sealed class CreateHolidayCommandHandler : IRequestHandler<CreateHolidayCommand, Result<Guid>>
{
    private readonly IWriteRepository<Holiday> _holidays;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateHolidayCommandHandler(
        IWriteRepository<Holiday> holidays,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _holidays = holidays;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateHolidayCommand request, CancellationToken cancellationToken)
    {
        var result = Holiday.Create(
            _tenantContext.TenantId,
            new LocationId(request.LocationId),
            request.Date,
            request.Name,
            _dateTimeProvider.UtcNow,
            _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _holidays.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
