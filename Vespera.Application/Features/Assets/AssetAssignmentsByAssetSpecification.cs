using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class AssetAssignmentsByAssetSpecification : ISpecification<AssetAssignment>
{
    public AssetAssignmentsByAssetSpecification(AssetId assetId)
    {
        Criteria = assignment => assignment.AssetId == assetId;

        // Not AssignedAt (DateTimeOffset): the SQLite provider (this repo's dev/test fallback)
        // can't translate ORDER BY on DateTimeOffset columns — see the identical note on
        // PendingAssetRecoveriesSpecification.
        OrderBy = [(assignment => (object)assignment.Id, true)];
    }

    public Expression<Func<AssetAssignment, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AssetAssignment, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AssetAssignment, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging => null;
}
