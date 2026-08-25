using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class DesignationsPagedSpecification : ISpecification<Designation>
{
    public DesignationsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = designation => designation.TenantId == tenantId && !designation.IsDeleted;
        OrderBy = [(designation => (object)designation.Title, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Designation, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Designation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Designation, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
