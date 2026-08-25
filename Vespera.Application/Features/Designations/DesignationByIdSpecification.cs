using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class DesignationByIdSpecification : ISpecification<Designation>
{
    public DesignationByIdSpecification(TenantId tenantId, DesignationId designationId)
    {
        Criteria = designation => designation.TenantId == tenantId && designation.Id == designationId && !designation.IsDeleted;
    }

    public Expression<Func<Designation, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Designation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Designation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
