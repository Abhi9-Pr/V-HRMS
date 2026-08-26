using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Every still-actionable (<c>Pending</c> or <c>Approved</c>) request for one employee —
/// an employee cannot be on two leaves (of any type) at once. <c>Period</c> overlap itself is
/// checked client-side after materializing: it's an opaque converted column (same limitation
/// <c>DelegationsByDelegatorSpecification</c> documents for <c>ProxyDelegation.Validity</c>).</summary>
public sealed class OverlappingLeaveRequestsSpecification : ISpecification<LeaveRequest>
{
    public OverlappingLeaveRequestsSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = r => r.TenantId == tenantId && r.EmployeeId == employeeId
            && (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved);
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
