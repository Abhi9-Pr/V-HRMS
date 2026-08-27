using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class CelebrationsByTenantSpecification : ISpecification<Celebration>
{
    public CelebrationsByTenantSpecification(TenantId tenantId)
    {
        Criteria = celebration => celebration.TenantId == tenantId;
    }

    public Expression<Func<Celebration, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Celebration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Celebration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
