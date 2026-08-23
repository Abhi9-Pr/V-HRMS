using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class GetOnboardingDraftByIdQueryHandler : IRequestHandler<GetOnboardingDraftByIdQuery, Result<OnboardingDraftDto>>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;

    public GetOnboardingDraftByIdQueryHandler(IReadRepository<OnboardingDraft> drafts, ITenantContext tenantContext)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OnboardingDraftDto>> Handle(GetOnboardingDraftByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.Id));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<OnboardingDraftDto>(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        var dto = new OnboardingDraftDto(
            draft.Id.Value,
            draft.Status.ToString(),
            draft.CurrentStep.ToString(),
            draft.FirstName,
            draft.LastName,
            draft.WorkEmail?.Value,
            draft.Phone?.Value,
            draft.DateOfBirth,
            draft.DepartmentId?.Value,
            draft.DesignationId?.Value,
            draft.LocationId?.Value,
            draft.DateOfJoining,
            draft.ConvertedEmployeeId?.Value,
            draft.Documents
                .Select(document => new OnboardingDraftDocumentDto(
                    document.Id.Value, document.DocumentType.ToString(), document.ScanStatus.ToString(),
                    document.IsOcrConfirmed, document.OcrConfidence))
                .ToList(),
            draft.ConsentRecords
                .Select(consent => new OnboardingDraftConsentDto(consent.ConsentType.ToString(), consent.Granted, consent.GrantedAt))
                .ToList());

        return Result.Success(dto);
    }
}
