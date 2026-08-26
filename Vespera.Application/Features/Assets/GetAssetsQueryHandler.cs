using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, Result<PagedResult<AssetDto>>>
{
    private readonly IReadRepository<Asset> _assets;
    private readonly ITenantContext _tenantContext;

    public GetAssetsQueryHandler(IReadRepository<Asset> assets, ITenantContext tenantContext)
    {
        _assets = assets;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<AssetDto>>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
    {
        var specification = new AssetsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var assets = await _assets.ListAsync(specification, cancellationToken);
        var totalCount = await _assets.CountAsync(specification, cancellationToken);

        var items = assets.Adapt<List<AssetDto>>();

        return Result.Success(new PagedResult<AssetDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
