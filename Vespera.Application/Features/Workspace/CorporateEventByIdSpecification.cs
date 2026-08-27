using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class CorporateEventByIdSpecification : ISpecification<CorporateEvent>
{
    public CorporateEventByIdSpecification(TenantId tenantId, CorporateEventId corporateEventId)
    {
        Criteria = corporateEvent =>
            corporateEvent.TenantId == tenantId && corporateEvent.Id == corporateEventId && !corporateEvent.IsDeleted;
    }

    public Expression<Func<CorporateEvent, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<CorporateEvent, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<CorporateEvent, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
