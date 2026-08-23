using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Attendance.Regularizations;

/// <summary>Every delegation (revoked or not, any scope, any date range) for one delegator —
/// filtered by <c>Criteria</c> only on the SQL-translatable predicate (<c>DelegatorId</c>);
/// active-on-date and scope filtering happen client-side in
/// <see cref="RegularizationApproverResolver"/>, since <c>Validity</c> is an opaque converted
/// column (see <c>ProxyDelegationConfiguration</c>), the same limitation
/// <c>RostersByEmployeesAndStatusSpecification</c> already hit for <c>ShiftRoster.Period</c>.</summary>
public sealed class DelegationsByDelegatorSpecification : ISpecification<ProxyDelegation>
{
    public DelegationsByDelegatorSpecification(EmployeeId delegatorId)
    {
        Criteria = d => d.DelegatorId == delegatorId && !d.IsRevoked;
    }

    public Expression<Func<ProxyDelegation, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ProxyDelegation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ProxyDelegation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
