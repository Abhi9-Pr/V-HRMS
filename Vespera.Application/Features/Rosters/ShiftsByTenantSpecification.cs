using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Rosters;

/// <summary>Every non-deleted shift for the tenant, unpaged — a roster query needs the whole
/// small reference-data set at once to resolve shift names for the days it renders.</summary>
public sealed class ShiftsByTenantSpecification : ISpecification<Shift>
{
    public ShiftsByTenantSpecification(TenantId tenantId)
    {
        Criteria = shift => shift.TenantId == tenantId && !shift.IsDeleted;
    }

    public Expression<Func<Shift, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Shift, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Shift, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
