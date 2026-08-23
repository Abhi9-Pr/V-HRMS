using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class ShiftByIdSpecification : ISpecification<Shift>
{
    public ShiftByIdSpecification(TenantId tenantId, ShiftId shiftId)
    {
        Criteria = shift => shift.TenantId == tenantId && shift.Id == shiftId && !shift.IsDeleted;
    }

    public Expression<Func<Shift, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Shift, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Shift, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
