using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Every blackout period for a tenant — <c>AppliesTo</c>/<c>Overlaps</c> are checked
/// client-side after materializing, same reasoning as <see cref="OverlappingLeaveRequestsSpecification"/>.</summary>
public sealed class BlackoutPeriodsByTenantSpecification : ISpecification<BlackoutPeriod>
{
    public BlackoutPeriodsByTenantSpecification(TenantId tenantId)
    {
        Criteria = b => b.TenantId == tenantId && !b.IsDeleted;
    }

    public Expression<Func<BlackoutPeriod, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<BlackoutPeriod, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<BlackoutPeriod, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
