using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.OrgChart;

/// <summary>Every non-deleted designation for the tenant, unpaged — used to resolve designation
/// titles for org chart nodes in one bulk query instead of one lookup per node.</summary>
public sealed class DesignationsByTenantSpecification : ISpecification<Designation>
{
    public DesignationsByTenantSpecification(TenantId tenantId)
    {
        Criteria = designation => designation.TenantId == tenantId && !designation.IsDeleted;
    }

    public Expression<Func<Designation, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Designation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Designation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
