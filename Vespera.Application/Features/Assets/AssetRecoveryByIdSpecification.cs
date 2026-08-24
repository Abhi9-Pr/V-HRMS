using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class AssetRecoveryByIdSpecification : ISpecification<AssetRecovery>
{
    public AssetRecoveryByIdSpecification(AssetRecoveryId recoveryId)
    {
        Criteria = recovery => recovery.Id == recoveryId;
    }

    public Expression<Func<AssetRecovery, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<AssetRecovery, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AssetRecovery, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
