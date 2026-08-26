using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class LeaveTypesByTenantSpecification : ISpecification<LeaveType>
{
    public LeaveTypesByTenantSpecification(TenantId tenantId)
    {
        Criteria = t => t.TenantId == tenantId && !t.IsDeleted;
    }

    public Expression<Func<LeaveType, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveType, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveType, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
