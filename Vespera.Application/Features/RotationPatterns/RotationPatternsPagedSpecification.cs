using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class RotationPatternsPagedSpecification : ISpecification<RotationPattern>
{
    public RotationPatternsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = pattern => pattern.TenantId == tenantId && !pattern.IsDeleted;
        OrderBy = [(pattern => (object)pattern.Name, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<RotationPattern, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<RotationPattern, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<RotationPattern, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
