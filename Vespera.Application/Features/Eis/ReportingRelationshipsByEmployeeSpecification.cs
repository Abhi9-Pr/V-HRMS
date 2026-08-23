using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

/// <summary>Every reporting-relationship row (active, future-dated, or closed) for one employee —
/// the full set a new candidate line must be checked against for overlap via
/// <see cref="Vespera.Domain.Common.EffectiveDatedTimeline.EnsureNoOverlap{TId, T}"/>, and the set
/// returned to a caller listing an employee's reporting history.</summary>
public sealed class ReportingRelationshipsByEmployeeSpecification : ISpecification<ReportingRelationship>
{
    public ReportingRelationshipsByEmployeeSpecification(EmployeeId employeeId)
    {
        Criteria = rr => rr.EmployeeId == employeeId;
    }

    public Expression<Func<ReportingRelationship, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ReportingRelationship, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ReportingRelationship, object>> KeySelector, bool Descending)> OrderBy { get; } =
        [(rr => (object)rr.ValidFrom, true)];

    public (int Skip, int Take)? Paging => null;
}
