using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class OffboardingChecklistByEmployeeIdSpecification : ISpecification<OffboardingChecklist>
{
    public OffboardingChecklistByEmployeeIdSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = checklist => checklist.TenantId == tenantId && checklist.EmployeeId == employeeId && !checklist.IsDeleted;
    }

    public Expression<Func<OffboardingChecklist, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<OffboardingChecklist, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<OffboardingChecklist, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
