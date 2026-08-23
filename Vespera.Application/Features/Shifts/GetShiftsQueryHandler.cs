using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, Result<PagedResult<ShiftDto>>>
{
    private readonly IReadRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;

    public GetShiftsQueryHandler(IReadRepository<Shift> shifts, ITenantContext tenantContext)
    {
        _shifts = shifts;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<ShiftDto>>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
    {
        var specification = new ShiftsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var shifts = await _shifts.ListAsync(specification, cancellationToken);
        var totalCount = await _shifts.CountAsync(specification, cancellationToken);

        var items = shifts.Adapt<List<ShiftDto>>();

        return Result.Success(new PagedResult<ShiftDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
