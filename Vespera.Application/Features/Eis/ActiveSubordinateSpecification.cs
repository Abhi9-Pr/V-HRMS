using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

/// <summary>True when <paramref name="employeeId"/> reports to <paramref name="managerId"/> as of
/// <paramref name="asOf"/>, per the effective-dated reporting hierarchy. Backs the "subordinate vs
/// self vs any" resource-based authorization check (see Api's SubordinateOrSelfAuthorizationHandler).</summary>
public sealed class ActiveSubordinateSpecification : ISpecification<ReportingRelationship>
{
    public ActiveSubordinateSpecification(EmployeeId managerId, EmployeeId employeeId, DateOnly asOf)
    {
        Criteria = rr =>
            rr.ManagerId == managerId && rr.EmployeeId == employeeId &&
            rr.ValidFrom <= asOf && (rr.ValidTo == null || rr.ValidTo >= asOf);
    }

    public Expression<Func<ReportingRelationship, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ReportingRelationship, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ReportingRelationship, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
