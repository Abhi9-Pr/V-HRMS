using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class AssetByIdSpecification : ISpecification<Asset>
{
    public AssetByIdSpecification(AssetId assetId)
    {
        Criteria = asset => asset.Id == assetId;
    }

    public Expression<Func<Asset, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Asset, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Asset, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
