using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, Result<PagedResult<HolidayDto>>>
{
    private readonly IReadRepository<Holiday> _holidays;
    private readonly ITenantContext _tenantContext;

    public GetHolidaysQueryHandler(IReadRepository<Holiday> holidays, ITenantContext tenantContext)
    {
        _holidays = holidays;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<HolidayDto>>> Handle(GetHolidaysQuery request, CancellationToken cancellationToken)
    {
        var specification = new HolidaysPagedSpecification(_tenantContext.TenantId, request.Paging, request.LocationId);

        var holidays = await _holidays.ListAsync(specification, cancellationToken);
        var totalCount = await _holidays.CountAsync(specification, cancellationToken);

        var items = holidays.Adapt<List<HolidayDto>>();

        return Result.Success(new PagedResult<HolidayDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
