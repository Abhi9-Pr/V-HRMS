using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed class QuarantinedBiometricPunchesPagedSpecification : ISpecification<QuarantinedBiometricPunch>
{
    public QuarantinedBiometricPunchesPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = entry => entry.TenantId == tenantId && entry.Status == QuarantinedBiometricPunchStatus.Pending;

        // Not PunchedAtUtc: Sqlite (used in tests and the dev fallback) can't translate ORDER BY
        // on a DateTimeOffset column at all — see BiometricDevicesPagedSpecification's identical
        // reasoning. DeviceUserId is a stable, translatable sort key for this small admin list.
        OrderBy = [(entry => (object)entry.DeviceUserId, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<QuarantinedBiometricPunch, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<QuarantinedBiometricPunch, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<QuarantinedBiometricPunch, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
