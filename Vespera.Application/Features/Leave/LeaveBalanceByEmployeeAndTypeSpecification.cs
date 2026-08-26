using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class LeaveBalanceByEmployeeAndTypeSpecification : ISpecification<LeaveBalance>
{
    public LeaveBalanceByEmployeeAndTypeSpecification(TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId)
    {
        Criteria = b => b.TenantId == tenantId && b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId;
    }

    public Expression<Func<LeaveBalance, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveBalance, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveBalance, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
