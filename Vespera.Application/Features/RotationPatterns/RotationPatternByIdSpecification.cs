using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class RotationPatternByIdSpecification : ISpecification<RotationPattern>
{
    public RotationPatternByIdSpecification(TenantId tenantId, RotationPatternId rotationPatternId)
    {
        Criteria = pattern => pattern.TenantId == tenantId && pattern.Id == rotationPatternId && !pattern.IsDeleted;
    }

    public Expression<Func<RotationPattern, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<RotationPattern, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<RotationPattern, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
