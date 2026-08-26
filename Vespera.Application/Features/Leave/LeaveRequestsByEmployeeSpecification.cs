using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Unordered — the handler sorts by <c>CreatedAt</c> client-side after materializing,
/// since SQLite (the local/integration-test provider) can't translate an <c>ORDER BY</c> over a
/// <c>DateTimeOffset</c> column; Postgres could, but the specification stays provider-neutral.</summary>
public sealed class LeaveRequestsByEmployeeSpecification : ISpecification<LeaveRequest>
{
    public LeaveRequestsByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = r => r.TenantId == tenantId && r.EmployeeId == employeeId;
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
