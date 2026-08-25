using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

/// <summary>Same scan-then-upload-then-attach orchestration as
/// <c>UploadEmployeeDocumentCommandHandler</c>, kept as an independent handler rather than a
/// shared helper: the two operate on different aggregates (<see cref="OnboardingDraft"/> vs
/// <see cref="Employee"/>) with different lookup specifications, so the only genuinely shared
/// logic is three lines of scan/upload calls — not enough to justify an extra layer of
/// indirection over just reading the two handlers side by side.</summary>
public sealed class UploadOnboardingDocumentCommandHandler : IRequestHandler<UploadOnboardingDocumentCommand, Result<Guid>>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly IVirusScanner _virusScanner;

    public UploadOnboardingDocumentCommandHandler(
        IReadRepository<OnboardingDraft> drafts,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IVirusScanner virusScanner)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _virusScanner = virusScanner;
    }

    public async Task<Result<Guid>> Handle(UploadOnboardingDocumentCommand request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.OnboardingDraftId));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<Guid>(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        // Scanned before anything is written to storage — an infected file is never persisted,
        // not even briefly.
        var scanResult = await _virusScanner.ScanAsync(new MemoryStream(request.Content), cancellationToken);
        if (scanResult == ScanResult.Infected)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "employee_document.infected", "The uploaded file failed a virus scan and was not stored."));
        }

        var storageKey = await _fileStorage.UploadAsync(request.FileName, new MemoryStream(request.Content), cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        var document = draft.AddDocument(request.DocumentType, storageKey, now);
        document.MarkScanned(scanResult == ScanResult.Clean ? DocumentScanStatus.Clean : DocumentScanStatus.Failed);

        return Result.Success(document.Id.Value);
    }
}
