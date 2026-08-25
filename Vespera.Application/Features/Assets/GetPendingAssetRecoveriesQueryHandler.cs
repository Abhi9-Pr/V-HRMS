using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class GetPendingAssetRecoveriesQueryHandler
    : IRequestHandler<GetPendingAssetRecoveriesQuery, Result<PagedResult<AssetRecoveryDto>>>
{
    private readonly IReadRepository<AssetRecovery> _recoveries;
    private readonly ITenantContext _tenantContext;

    public GetPendingAssetRecoveriesQueryHandler(IReadRepository<AssetRecovery> recoveries, ITenantContext tenantContext)
    {
        _recoveries = recoveries;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<AssetRecoveryDto>>> Handle(
        GetPendingAssetRecoveriesQuery request, CancellationToken cancellationToken)
    {
        var specification = new PendingAssetRecoveriesSpecification(_tenantContext.TenantId, request.Paging);

        var recoveries = await _recoveries.ListAsync(specification, cancellationToken);
        var totalCount = await _recoveries.CountAsync(specification, cancellationToken);

        var items = recoveries.Adapt<List<AssetRecoveryDto>>();

        return Result.Success(new PagedResult<AssetRecoveryDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
