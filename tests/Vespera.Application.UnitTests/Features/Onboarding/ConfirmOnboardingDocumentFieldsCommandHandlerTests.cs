using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class ConfirmOnboardingDocumentFieldsCommandHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public ConfirmOnboardingDocumentFieldsCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private ConfirmOnboardingDocumentFieldsCommandHandler CreateHandler() => new(_drafts, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Apply_Only_The_Explicitly_Confirmed_Value_Not_The_Raw_Ocr_Suggestion()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        draft.UpdatePersonalDetails(
            "Placeholder", "Placeholder", EmailAddress.Create("newhire@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), DateTimeOffset.UtcNow, "hr@vespera.test");
        var document = draft.AddDocument(EmployeeDocumentType.Id, "storage-key-1", DateTimeOffset.UtcNow);
        document.RecordOcrSuggestion("{\"firstName\":\"WrongOcrGuess\"}", 0.5);
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(
            new ConfirmOnboardingDocumentFieldsCommand(draft.Id.Value, document.Id.Value, "ActualCorrectName", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.FirstName.Should().Be("ActualCorrectName");
        document.IsOcrConfirmed.Should().BeTrue();
        document.ConfirmedFieldsJson.Should().Contain("ActualCorrectName").And.NotContain("WrongOcrGuess");
    }

    [Fact]
    public async Task Handle_Should_Leave_Personal_Details_Untouched_When_No_Fields_Are_Confirmed()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        var document = draft.AddDocument(EmployeeDocumentType.Id, "storage-key-1", DateTimeOffset.UtcNow);
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(
            new ConfirmOnboardingDocumentFieldsCommand(draft.Id.Value, document.Id.Value, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.IsOcrConfirmed.Should().BeTrue();
        draft.FirstName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Confirming_Identity_Fields_Before_Personal_Details_Step_Was_Started()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        var document = draft.AddDocument(EmployeeDocumentType.Id, "storage-key-1", DateTimeOffset.UtcNow);
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(
            new ConfirmOnboardingDocumentFieldsCommand(draft.Id.Value, document.Id.Value, "Ada", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("onboarding_draft.personal_details_not_started");
    }
}
