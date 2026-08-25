using System.Text.Json;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

/// <summary>
/// OCR results are suggestions, never auto-committed — this handler stores what the OCR engine
/// found on the <see cref="EmployeeDocument"/> itself (<see cref="EmployeeDocument.RecordOcrSuggestion"/>)
/// and hands it back to the caller; it never writes into any <see cref="OnboardingDraft"/> field.
/// Only <see cref="ConfirmOnboardingDocumentFieldsCommandHandler"/> ever changes draft data, and
/// only with values the caller explicitly confirmed.
/// </summary>
public sealed class ExtractOnboardingDocumentFieldsCommandHandler : IRequestHandler<ExtractOnboardingDocumentFieldsCommand, Result<OcrExtractionResultDto>>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;
    private readonly IFileStorage _fileStorage;
    private readonly IDocumentOcrService _ocrService;

    public ExtractOnboardingDocumentFieldsCommandHandler(
        IReadRepository<OnboardingDraft> drafts,
        ITenantContext tenantContext,
        IFileStorage fileStorage,
        IDocumentOcrService ocrService)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
        _fileStorage = fileStorage;
        _ocrService = ocrService;
    }

    public async Task<Result<OcrExtractionResultDto>> Handle(ExtractOnboardingDocumentFieldsCommand request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.OnboardingDraftId));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<OcrExtractionResultDto>(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        var document = draft.Documents.FirstOrDefault(d => d.Id == new EmployeeDocumentId(request.EmployeeDocumentId));
        if (document is null)
        {
            return Result.Failure<OcrExtractionResultDto>(Error.NotFound("employee_document.not_found", "Document not found."));
        }

        // Explicit consent must exist before any ID document is processed by OCR — checked here,
        // not as an OnboardingDraft invariant, since it's workflow sequencing across the draft's
        // own child collections (consent vs. documents), not a single-aggregate rule.
        var hasDataProcessingConsent = draft.ConsentRecords.Any(c => c.ConsentType == ConsentType.DataProcessing && c.Granted);
        if (!hasDataProcessingConsent)
        {
            return Result.Failure<OcrExtractionResultDto>(Error.Forbidden(
                "onboarding.consent_required", "Data-processing consent must be recorded before this document can be processed."));
        }

        await using var content = await _fileStorage.DownloadAsync(document.FileReference, cancellationToken);
        var ocrResult = await _ocrService.ExtractAsync(content, cancellationToken);

        var fieldsJson = JsonSerializer.Serialize(ocrResult.Fields);
        var recordResult = document.RecordOcrSuggestion(fieldsJson, ocrResult.Confidence);
        if (recordResult.IsFailure)
        {
            return Result.Failure<OcrExtractionResultDto>(recordResult.Error);
        }

        return Result.Success(new OcrExtractionResultDto(ocrResult.ExtractedText, ocrResult.Fields, ocrResult.Confidence));
    }
}
