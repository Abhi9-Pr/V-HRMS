using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CreatePublicHolidayCommandHandler : IRequestHandler<CreatePublicHolidayCommand, Result<Guid>>
{
    private readonly IWriteRepository<PublicHoliday> _holidays;
    private readonly ITenantContext _tenantContext;

    public CreatePublicHolidayCommandHandler(IWriteRepository<PublicHoliday> holidays, ITenantContext tenantContext)
    {
        _holidays = holidays;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreatePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        var result = PublicHoliday.Create(_tenantContext.TenantId, request.Date, request.Name);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _holidays.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
