using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class LeaveRequestByIdSpecification : ISpecification<LeaveRequest>
{
    public LeaveRequestByIdSpecification(TenantId tenantId, LeaveRequestId id)
    {
        Criteria = r => r.TenantId == tenantId && r.Id == id;
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
