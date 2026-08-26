using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetPublicHolidaysQueryHandler : IRequestHandler<GetPublicHolidaysQuery, Result<PagedResult<PublicHolidayDto>>>
{
    private readonly IReadRepository<PublicHoliday> _holidays;
    private readonly ITenantContext _tenantContext;

    public GetPublicHolidaysQueryHandler(IReadRepository<PublicHoliday> holidays, ITenantContext tenantContext)
    {
        _holidays = holidays;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<PublicHolidayDto>>> Handle(GetPublicHolidaysQuery request, CancellationToken cancellationToken)
    {
        var specification = new PublicHolidaysPagedSpecification(_tenantContext.TenantId, request.Paging);

        var holidays = await _holidays.ListAsync(specification, cancellationToken);
        var totalCount = await _holidays.CountAsync(specification, cancellationToken);

        var items = holidays.Adapt<List<PublicHolidayDto>>();

        return Result.Success(new PagedResult<PublicHolidayDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
