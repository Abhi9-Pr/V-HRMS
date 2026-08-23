using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed class GetQuarantinedPunchesQueryHandler : IRequestHandler<GetQuarantinedPunchesQuery, Result<PagedResult<QuarantinedBiometricPunchDto>>>
{
    private readonly IReadRepository<QuarantinedBiometricPunch> _entries;
    private readonly ITenantContext _tenantContext;

    public GetQuarantinedPunchesQueryHandler(IReadRepository<QuarantinedBiometricPunch> entries, ITenantContext tenantContext)
    {
        _entries = entries;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<QuarantinedBiometricPunchDto>>> Handle(
        GetQuarantinedPunchesQuery request, CancellationToken cancellationToken)
    {
        var specification = new QuarantinedBiometricPunchesPagedSpecification(_tenantContext.TenantId, request.Paging);

        var entries = await _entries.ListAsync(specification, cancellationToken);
        var totalCount = await _entries.CountAsync(specification, cancellationToken);

        var items = entries.Adapt<List<QuarantinedBiometricPunchDto>>();

        return Result.Success(new PagedResult<QuarantinedBiometricPunchDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
