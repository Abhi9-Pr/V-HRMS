using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class JobRequisitionsPagedSpecification : ISpecification<JobRequisition>
{
    public JobRequisitionsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = requisition => requisition.TenantId == tenantId && !requisition.IsDeleted;
        OrderBy = [(requisition => (object)requisition.Title, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<JobRequisition, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<JobRequisition, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<JobRequisition, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
