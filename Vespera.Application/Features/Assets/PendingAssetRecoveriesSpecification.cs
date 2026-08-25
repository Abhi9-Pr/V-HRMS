using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class PendingAssetRecoveriesSpecification : ISpecification<AssetRecovery>
{
    public PendingAssetRecoveriesSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = recovery => recovery.TenantId == tenantId && recovery.Status != AssetRecoveryStatus.Completed;

        // Not InitiatedAt (DateTimeOffset): the SQLite provider (this repo's dev/test fallback —
        // see VesperaPersistenceServiceCollectionExtensions) can't translate ORDER BY on
        // DateTimeOffset columns at all, and throws NotSupportedException rather than falling back
        // to client-side ordering. Id gives stable, portable paging across every supported provider.
        OrderBy = [(recovery => (object)recovery.Id, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<AssetRecovery, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AssetRecovery, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AssetRecovery, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
