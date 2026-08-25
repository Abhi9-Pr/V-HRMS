using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed class BiometricDevicesPagedSpecification : ISpecification<BiometricDevice>
{
    public BiometricDevicesPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = device => device.TenantId == tenantId && !device.IsDeleted;

        // Not CreatedAt: Sqlite (used in tests and the dev fallback) can't translate ORDER BY on
        // a DateTimeOffset column at all ("SQLite does not support expressions of type
        // 'DateTimeOffset' in ORDER BY clauses") — the same class of translation gap the
        // retention/offboarding sweeps document for other predicate shapes. Host is a stable,
        // translatable sort key for this small admin list.
        OrderBy = [(device => (object)device.Host, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<BiometricDevice, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<BiometricDevice, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<BiometricDevice, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
