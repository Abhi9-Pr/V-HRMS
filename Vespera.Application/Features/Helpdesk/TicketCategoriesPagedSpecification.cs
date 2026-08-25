using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class TicketCategoriesPagedSpecification : ISpecification<TicketCategory>
{
    public TicketCategoriesPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = category => category.TenantId == tenantId && !category.IsDeleted;
        OrderBy = [(category => (object)category.Name, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<TicketCategory, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<TicketCategory, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<TicketCategory, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
