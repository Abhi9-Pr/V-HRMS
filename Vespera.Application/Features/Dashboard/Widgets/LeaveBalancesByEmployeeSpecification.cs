using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed class LeaveBalancesByEmployeeSpecification : ISpecification<LeaveBalance>
{
    public LeaveBalancesByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = balance => balance.TenantId == tenantId && balance.EmployeeId == employeeId;
    }

    public Expression<Func<LeaveBalance, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveBalance, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveBalance, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
