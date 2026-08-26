using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class SlaPoliciesPagedSpecification : ISpecification<SlaPolicy>
{
    public SlaPoliciesPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = policy => policy.TenantId == tenantId && !policy.IsDeleted;
        OrderBy = [(policy => (object)policy.Name, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<SlaPolicy, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<SlaPolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SlaPolicy, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
