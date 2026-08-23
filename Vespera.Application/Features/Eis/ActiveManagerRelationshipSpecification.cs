using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

/// <summary>Resolves the single reporting-relationship row that names
/// <paramref name="employeeId"/>'s active manager as of <paramref name="asOf"/> — i.e. "who does
/// this employee report to on this date." Used both by cycle detection (walking a manager chain,
/// see CreateReportingRelationshipCommandHandler) and by anything else that needs a single
/// employee's current manager.</summary>
public sealed class ActiveManagerRelationshipSpecification : ISpecification<ReportingRelationship>
{
    public ActiveManagerRelationshipSpecification(EmployeeId employeeId, DateOnly asOf)
    {
        Criteria = rr =>
            rr.EmployeeId == employeeId &&
            rr.ValidFrom <= asOf && (rr.ValidTo == null || rr.ValidTo >= asOf);
    }

    public Expression<Func<ReportingRelationship, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ReportingRelationship, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ReportingRelationship, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
