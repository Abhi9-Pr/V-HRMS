using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.OrgChart;

/// <summary>Every reporting-relationship row for the tenant that is active on <paramref name="asOf"/> —
/// the bulk-load this query builds its manager→subordinates adjacency from. Deliberately
/// unfiltered by employee: loading everything in one query and building the tree in memory avoids
/// an N+1 per-node lookup.</summary>
public sealed class ActiveReportingRelationshipsSpecification : ISpecification<ReportingRelationship>
{
    public ActiveReportingRelationshipsSpecification(TenantId tenantId, DateOnly asOf)
    {
        Criteria = rr =>
            rr.TenantId == tenantId &&
            rr.ValidFrom <= asOf && (rr.ValidTo == null || rr.ValidTo >= asOf);
    }

    public Expression<Func<ReportingRelationship, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<ReportingRelationship, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ReportingRelationship, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
