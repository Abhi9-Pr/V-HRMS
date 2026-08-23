using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

/// <summary>Every one employee's attendance day, unfiltered by date, unpaged/unordered —
/// <see cref="DeltaSyncQueryHandlerBase{TRequest,TEntity,TDto}"/> applies the since-cutoff, sorts,
/// and pages the (small, one-employee-scoped) result set in memory instead, to avoid comparing or
/// ordering by a <c>DateTimeOffset</c> column at the SQL level (unsupported by this codebase's
/// Sqlite test/dev-fallback provider).</summary>
public sealed class AttendanceDaysByEmployeeSinceSpecification : ISpecification<AttendanceDay>
{
    public AttendanceDaysByEmployeeSinceSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = day => day.TenantId == tenantId && day.EmployeeId == employeeId;
    }

    public Expression<Func<AttendanceDay, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AttendanceDay, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AttendanceDay, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
