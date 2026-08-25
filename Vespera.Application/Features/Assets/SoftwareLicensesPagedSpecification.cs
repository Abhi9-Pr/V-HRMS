using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class SoftwareLicensesPagedSpecification : ISpecification<SoftwareLicense>
{
    public SoftwareLicensesPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = license => license.TenantId == tenantId && !license.IsDeleted;
        OrderBy = [(license => (object)license.ProductName, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<SoftwareLicense, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<SoftwareLicense, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SoftwareLicense, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
