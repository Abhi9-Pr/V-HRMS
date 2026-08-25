using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class ExtractOnboardingDocumentFieldsCommandHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly IDocumentOcrService _ocrService = Substitute.For<IDocumentOcrService>();
    private readonly TenantId _tenantId = TenantId.New();

    public ExtractOnboardingDocumentFieldsCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private ExtractOnboardingDocumentFieldsCommandHandler CreateHandler() => new(_drafts, _tenantContext, _fileStorage, _ocrService);

    private (OnboardingDraft Draft, EmployeeDocument Document) CreateDraftWithDocument()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        var document = draft.AddDocument(EmployeeDocumentType.Id, "storage-key-1", DateTimeOffset.UtcNow);
        return (draft, document);
    }

    [Fact]
    public async Task Handle_Should_Reject_Without_Ever_Calling_Ocr_When_No_DataProcessing_Consent_Exists()
    {
        var (draft, document) = CreateDraftWithDocument();
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(
            new ExtractOnboardingDocumentFieldsCommand(draft.Id.Value, document.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("onboarding.consent_required");
        await _ocrService.DidNotReceiveWithAnyArgs().ExtractAsync(default!, default);
        await _fileStorage.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default);
        document.OcrSuggestedFieldsJson.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Call_Ocr_And_Record_The_Suggestion_When_Consent_Exists()
    {
        var (draft, document) = CreateDraftWithDocument();
        draft.RecordConsent(ConsentType.DataProcessing, DateTimeOffset.UtcNow);
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);
        _fileStorage.DownloadAsync(document.FileReference, Arg.Any<CancellationToken>()).Returns(new MemoryStream([1, 2, 3]));
        var ocrResult = new OcrResult("scanned text", new Dictionary<string, string> { ["firstName"] = "Ocr Guess" }, 0.77);
        _ocrService.ExtractAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(ocrResult);

        var result = await CreateHandler().Handle(
            new ExtractOnboardingDocumentFieldsCommand(draft.Id.Value, document.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Fields["firstName"].Should().Be("Ocr Guess");
        document.OcrSuggestedFieldsJson.Should().Contain("Ocr Guess");
        document.OcrConfidence.Should().Be(0.77);
        // Extraction alone must never touch the draft's own personal-details snapshot.
        draft.FirstName.Should().BeNull();
    }
}
