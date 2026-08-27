using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class UpcomingCorporateEventsSpecification : ISpecification<CorporateEvent>
{
    public UpcomingCorporateEventsSpecification(TenantId tenantId, DateTimeOffset asOf)
    {
        Criteria = corporateEvent =>
            corporateEvent.TenantId == tenantId && !corporateEvent.IsDeleted && !corporateEvent.IsCancelled && corporateEvent.EndsAt >= asOf;
        OrderBy = [(corporateEvent => (object)corporateEvent.StartsAt, false)];
    }

    public Expression<Func<CorporateEvent, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<CorporateEvent, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<CorporateEvent, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging => null;
}
