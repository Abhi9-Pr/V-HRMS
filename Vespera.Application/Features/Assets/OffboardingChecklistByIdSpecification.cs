using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class OffboardingChecklistByIdSpecification : ISpecification<OffboardingChecklist>
{
    public OffboardingChecklistByIdSpecification(OffboardingChecklistId checklistId)
    {
        Criteria = checklist => checklist.Id == checklistId;
    }

    public Expression<Func<OffboardingChecklist, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<OffboardingChecklist, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<OffboardingChecklist, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
