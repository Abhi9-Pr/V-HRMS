using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class LeaveTypeByIdSpecification : ISpecification<LeaveType>
{
    public LeaveTypeByIdSpecification(TenantId tenantId, LeaveTypeId id)
    {
        Criteria = t => t.TenantId == tenantId && t.Id == id && !t.IsDeleted;
    }

    public Expression<Func<LeaveType, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveType, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveType, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
