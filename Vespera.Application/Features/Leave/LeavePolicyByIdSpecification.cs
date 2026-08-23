using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class LeavePolicyByIdSpecification : ISpecification<LeavePolicy>
{
    public LeavePolicyByIdSpecification(TenantId tenantId, LeavePolicyId id)
    {
        Criteria = p => p.TenantId == tenantId && p.Id == id;
    }

    public Expression<Func<LeavePolicy, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeavePolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeavePolicy, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
