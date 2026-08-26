using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class BlackoutPeriodByIdSpecification : ISpecification<BlackoutPeriod>
{
    public BlackoutPeriodByIdSpecification(TenantId tenantId, BlackoutPeriodId id)
    {
        Criteria = b => b.TenantId == tenantId && b.Id == id && !b.IsDeleted;
    }

    public Expression<Func<BlackoutPeriod, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<BlackoutPeriod, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<BlackoutPeriod, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
