using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

/// <summary>Every designation for the tenant, unpaged — backs <see cref="GetDesignationsETagQuery"/>,
/// which needs the full matching set's RowVersions, not one page of them.</summary>
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
