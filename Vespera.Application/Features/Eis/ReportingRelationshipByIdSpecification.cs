using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class ReportingRelationshipByIdSpecification : ISpecification<ReportingRelationship>
{
    public ReportingRelationshipByIdSpecification(ReportingRelationshipId id)
    {
        Criteria = rr => rr.Id == id;
    }

    public Expression<Func<ReportingRelationship, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ReportingRelationship, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ReportingRelationship, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
