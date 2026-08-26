using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class AssetsPagedSpecification : ISpecification<Asset>
{
    public AssetsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = asset => asset.TenantId == tenantId && !asset.IsDeleted;
        OrderBy = [(asset => (object)asset.AssetTag, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Asset, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Asset, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Asset, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
