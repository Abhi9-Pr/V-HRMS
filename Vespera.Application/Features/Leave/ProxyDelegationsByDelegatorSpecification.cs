using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Every delegation (active or revoked) for one delegator — for a management/history view.
/// The Attendance/Regularizations <c>DelegationsByDelegatorSpecification</c> is the narrower,
/// active-only variant used by approval-authorization resolution.</summary>
public sealed class ProxyDelegationsByDelegatorSpecification : ISpecification<ProxyDelegation>
{
    public ProxyDelegationsByDelegatorSpecification(EmployeeId delegatorId)
    {
        Criteria = d => d.DelegatorId == delegatorId;
    }

    public Expression<Func<ProxyDelegation, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ProxyDelegation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ProxyDelegation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
