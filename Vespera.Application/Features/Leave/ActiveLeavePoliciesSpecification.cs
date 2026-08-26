using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Every <see cref="LeavePolicy"/> (any tenant) whose effective-dated window covers
/// <c>asOf</c> — used cross-tenant by <c>LeaveAccrualHostedService</c> via
/// <see cref="Application.Abstractions.Persistence.IReadRepositoryAdmin{T}"/>.</summary>
public sealed class ActiveLeavePoliciesSpecification : ISpecification<LeavePolicy>
{
    public ActiveLeavePoliciesSpecification(DateOnly asOf)
    {
        Criteria = p => p.ValidFrom <= asOf && (p.ValidTo == null || p.ValidTo.Value >= asOf);
    }

    public Expression<Func<LeavePolicy, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<LeavePolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeavePolicy, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
