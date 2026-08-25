using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class OnboardingDraftByIdSpecification : ISpecification<OnboardingDraft>
{
    public OnboardingDraftByIdSpecification(TenantId tenantId, OnboardingDraftId draftId)
    {
        Criteria = draft => draft.TenantId == tenantId && draft.Id == draftId && !draft.IsDeleted;
    }

    public Expression<Func<OnboardingDraft, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<OnboardingDraft, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<OnboardingDraft, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
