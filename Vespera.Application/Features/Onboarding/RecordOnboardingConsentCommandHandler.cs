using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class RecordOnboardingConsentCommandHandler : IRequestHandler<RecordOnboardingConsentCommand, Result>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordOnboardingConsentCommandHandler(
        IReadRepository<OnboardingDraft> drafts, ITenantContext tenantContext, IDateTimeProvider dateTimeProvider)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RecordOnboardingConsentCommand request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.OnboardingDraftId));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        if (!Enum.TryParse<ConsentType>(request.ConsentType, ignoreCase: true, out var consentType))
        {
            return Result.Failure(Error.Validation("onboarding_draft.invalid_consent_type", "Unrecognized consent type."));
        }

        draft.RecordConsent(consentType, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
