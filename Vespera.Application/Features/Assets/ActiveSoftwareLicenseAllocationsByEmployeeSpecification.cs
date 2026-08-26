using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Assets;

public sealed class ActiveSoftwareLicenseAllocationsByEmployeeSpecification : ISpecification<SoftwareLicenseAllocation>
{
    public ActiveSoftwareLicenseAllocationsByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = allocation => allocation.TenantId == tenantId && allocation.EmployeeId == employeeId && allocation.ReleasedAt == null;
    }

    public Expression<Func<SoftwareLicenseAllocation, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<SoftwareLicenseAllocation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SoftwareLicenseAllocation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
