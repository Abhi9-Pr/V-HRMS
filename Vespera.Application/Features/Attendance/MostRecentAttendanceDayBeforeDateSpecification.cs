using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

/// <summary>The employee's most recent attendance day strictly before <paramref name="date"/> —
/// used by the mobile "impossible travel" check to find the last punch made on any prior day when
/// today's own attendance day has no punches yet.</summary>
public sealed class MostRecentAttendanceDayBeforeDateSpecification : ISpecification<AttendanceDay>
{
    public MostRecentAttendanceDayBeforeDateSpecification(TenantId tenantId, EmployeeId employeeId, DateOnly date)
    {
        Criteria = day => day.TenantId == tenantId && day.EmployeeId == employeeId && day.Date < date;
    }

    public Expression<Func<AttendanceDay, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AttendanceDay, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AttendanceDay, object>> KeySelector, bool Descending)> OrderBy { get; } =
        [(day => (object)day.Date, true)];

    public (int Skip, int Take)? Paging => (0, 1);
}
