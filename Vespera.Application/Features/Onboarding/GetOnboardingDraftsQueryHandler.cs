using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class GetOnboardingDraftsQueryHandler : IRequestHandler<GetOnboardingDraftsQuery, Result<PagedResult<OnboardingDraftSummaryDto>>>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;

    public GetOnboardingDraftsQueryHandler(IReadRepository<OnboardingDraft> drafts, ITenantContext tenantContext)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<OnboardingDraftSummaryDto>>> Handle(GetOnboardingDraftsQuery request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var drafts = await _drafts.ListAsync(specification, cancellationToken);
        var totalCount = await _drafts.CountAsync(specification, cancellationToken);

        var items = drafts
            .Select(draft => new OnboardingDraftSummaryDto(
                draft.Id.Value, draft.Status.ToString(), draft.CurrentStep.ToString(), draft.FirstName, draft.LastName, draft.CreatedAt))
            .ToList();

        return Result.Success(new PagedResult<OnboardingDraftSummaryDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
