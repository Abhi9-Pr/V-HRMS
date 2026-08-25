using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

public sealed class AttendanceDayByEmployeeAndDateSpecification : ISpecification<AttendanceDay>
{
    public AttendanceDayByEmployeeAndDateSpecification(TenantId tenantId, EmployeeId employeeId, DateOnly date)
    {
        Criteria = day => day.TenantId == tenantId && day.EmployeeId == employeeId && day.Date == date;
    }

    public Expression<Func<AttendanceDay, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AttendanceDay, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AttendanceDay, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
