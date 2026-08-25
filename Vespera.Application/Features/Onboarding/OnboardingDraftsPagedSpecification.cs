using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class OnboardingDraftsPagedSpecification : ISpecification<OnboardingDraft>
{
    public OnboardingDraftsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = draft => draft.TenantId == tenantId && !draft.IsDeleted;
        OrderBy = [(draft => (object)draft.CreatedAt, true)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<OnboardingDraft, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<OnboardingDraft, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<OnboardingDraft, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
