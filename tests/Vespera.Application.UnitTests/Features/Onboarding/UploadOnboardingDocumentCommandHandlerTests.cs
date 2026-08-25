using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class UploadOnboardingDocumentCommandHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly IVirusScanner _virusScanner = Substitute.For<IVirusScanner>();
    private readonly TenantId _tenantId = TenantId.New();

    public UploadOnboardingDocumentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UploadOnboardingDocumentCommandHandler CreateHandler() =>
        new(_drafts, _tenantContext, _dateTimeProvider, _fileStorage, _virusScanner);

    private OnboardingDraft CreateDraft() => OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;

    [Fact]
    public async Task Handle_Should_Reject_An_Infected_File_Without_Uploading_Or_Attaching_It()
    {
        var draft = CreateDraft();
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);
        _virusScanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(ScanResult.Infected);

        var result = await CreateHandler().Handle(
            new UploadOnboardingDocumentCommand(draft.Id.Value, EmployeeDocumentType.Id, "id.jpg", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.infected");
        draft.Documents.Should().BeEmpty();
        await _fileStorage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_Should_Attach_A_Clean_Document_Marked_Clean()
    {
        var draft = CreateDraft();
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);
        _virusScanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(ScanResult.Clean);
        _fileStorage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("storage-key-1");

        var result = await CreateHandler().Handle(
            new UploadOnboardingDocumentCommand(draft.Id.Value, EmployeeDocumentType.Id, "id.jpg", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.Documents.Should().ContainSingle();
        draft.Documents.Single().ScanStatus.Should().Be(DocumentScanStatus.Clean);
    }
}
