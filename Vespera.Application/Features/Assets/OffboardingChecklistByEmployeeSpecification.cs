using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using OffboardingChecklist = Vespera.Domain.Assets.OffboardingChecklist;

namespace Vespera.Application.Features.Assets;

public sealed class OffboardingChecklistByEmployeeSpecification : ISpecification<OffboardingChecklist>
{
    public OffboardingChecklistByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = checklist => checklist.TenantId == tenantId && checklist.EmployeeId == employeeId;
    }

    public Expression<Func<OffboardingChecklist, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<OffboardingChecklist, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<OffboardingChecklist, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
