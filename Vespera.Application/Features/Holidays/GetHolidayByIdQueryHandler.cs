using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed class GetHolidayByIdQueryHandler : IRequestHandler<GetHolidayByIdQuery, Result<HolidayDto>>
{
    private readonly IReadRepository<Holiday> _holidays;
    private readonly ITenantContext _tenantContext;

    public GetHolidayByIdQueryHandler(IReadRepository<Holiday> holidays, ITenantContext tenantContext)
    {
        _holidays = holidays;
        _tenantContext = tenantContext;
    }

    public async Task<Result<HolidayDto>> Handle(GetHolidayByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new HolidayByIdSpecification(_tenantContext.TenantId, new HolidayId(request.Id));

        var holiday = await _holidays.FirstOrDefaultAsync(specification, cancellationToken);
        if (holiday is null)
        {
            return Result.Failure<HolidayDto>(Error.NotFound("holiday.not_found", "Holiday not found."));
        }

        return Result.Success(holiday.Adapt<HolidayDto>());
    }
}
