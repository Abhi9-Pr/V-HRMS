using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class LeavePoliciesByTenantSpecification : ISpecification<LeavePolicy>
{
    public LeavePoliciesByTenantSpecification(TenantId tenantId)
    {
        Criteria = p => p.TenantId == tenantId;
    }

    public Expression<Func<LeavePolicy, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeavePolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeavePolicy, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
