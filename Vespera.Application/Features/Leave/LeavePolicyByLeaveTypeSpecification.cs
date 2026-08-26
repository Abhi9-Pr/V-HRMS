using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>The <see cref="LeavePolicy"/> in force for one leave type as of a given date — there is
/// at most one, since effective-dated windows for the same <c>(TenantId, LeaveTypeId)</c> don't
/// overlap by convention (same assumption <c>LeavePolicyConfiguration</c> and the seeder make).</summary>
public sealed class LeavePolicyByLeaveTypeSpecification : ISpecification<LeavePolicy>
{
    public LeavePolicyByLeaveTypeSpecification(TenantId tenantId, LeaveTypeId leaveTypeId, DateOnly asOf)
    {
        Criteria = p => p.TenantId == tenantId && p.LeaveTypeId == leaveTypeId
            && p.ValidFrom <= asOf && (p.ValidTo == null || p.ValidTo.Value >= asOf);
    }

    public Expression<Func<LeavePolicy, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeavePolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeavePolicy, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
