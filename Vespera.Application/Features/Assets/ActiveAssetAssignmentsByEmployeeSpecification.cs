using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Assets;

public sealed class ActiveAssetAssignmentsByEmployeeSpecification : ISpecification<AssetAssignment>
{
    public ActiveAssetAssignmentsByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = assignment => assignment.TenantId == tenantId && assignment.EmployeeId == employeeId && assignment.ReturnedAt == null;
    }

    public Expression<Func<AssetAssignment, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<AssetAssignment, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AssetAssignment, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
