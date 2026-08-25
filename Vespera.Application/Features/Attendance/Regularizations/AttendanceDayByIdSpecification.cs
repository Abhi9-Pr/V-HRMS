using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class AttendanceDayByIdSpecification : ISpecification<AttendanceDay>
{
    public AttendanceDayByIdSpecification(TenantId tenantId, AttendanceDayId id)
    {
        Criteria = d => d.TenantId == tenantId && d.Id == id;
    }

    public Expression<Func<AttendanceDay, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<AttendanceDay, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AttendanceDay, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
