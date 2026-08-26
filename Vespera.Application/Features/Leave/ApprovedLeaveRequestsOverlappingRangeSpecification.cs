using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Every approved request for the tenant — <c>Period</c> overlap against the requested
/// calendar range is checked client-side after materializing, same limitation as
/// <see cref="OverlappingLeaveRequestsSpecification"/>. Used by the team calendar; a tenant's
/// approved-request volume is small enough that filtering the whole set client-side is fine.</summary>
public sealed class ApprovedLeaveRequestsOverlappingRangeSpecification : ISpecification<LeaveRequest>
{
    public ApprovedLeaveRequestsOverlappingRangeSpecification(TenantId tenantId)
    {
        Criteria = r => r.TenantId == tenantId && r.Status == LeaveRequestStatus.Approved;
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
