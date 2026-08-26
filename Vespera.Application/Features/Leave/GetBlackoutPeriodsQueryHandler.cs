using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetBlackoutPeriodsQueryHandler : IRequestHandler<GetBlackoutPeriodsQuery, Result<IReadOnlyList<BlackoutPeriodDto>>>
{
    private readonly IReadRepository<BlackoutPeriod> _blackouts;
    private readonly ITenantContext _tenantContext;

    public GetBlackoutPeriodsQueryHandler(IReadRepository<BlackoutPeriod> blackouts, ITenantContext tenantContext)
    {
        _blackouts = blackouts;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<BlackoutPeriodDto>>> Handle(GetBlackoutPeriodsQuery request, CancellationToken cancellationToken)
    {
        var blackouts = await _blackouts.ListAsync(new BlackoutPeriodsByTenantSpecification(_tenantContext.TenantId), cancellationToken);
        var dtos = blackouts.Select(b => new BlackoutPeriodDto(b.Id.Value, b.Period.Start, b.Period.End, b.Reason, b.LeaveTypeId?.Value)).ToList();
        return Result.Success<IReadOnlyList<BlackoutPeriodDto>>(dtos);
    }
}
