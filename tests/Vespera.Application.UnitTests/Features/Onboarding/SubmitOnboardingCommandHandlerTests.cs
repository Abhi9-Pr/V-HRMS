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

public class SubmitOnboardingCommandHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly IWriteRepository<Employee> _employees = Substitute.For<IWriteRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public SubmitOnboardingCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private SubmitOnboardingCommandHandler CreateHandler() => new(_drafts, _employees, _tenantContext, _currentUser, _dateTimeProvider);

    private OnboardingDraft CreateCompleteDraft()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        draft.UpdatePersonalDetails(
            "Ada", "Lovelace", EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), DateTimeOffset.UtcNow, "hr@vespera.test");
        draft.UpdateEmploymentDetails(DepartmentId.New(), DesignationId.New(), LocationId.New(), new DateOnly(2026, 2, 1), DateTimeOffset.UtcNow, "hr@vespera.test");
        draft.AddDocument(EmployeeDocumentType.Id, "storage-key-1", DateTimeOffset.UtcNow);
        draft.RecordConsent(ConsentType.DataProcessing, DateTimeOffset.UtcNow);
        return draft;
    }

    [Fact]
    public async Task Handle_Should_Convert_A_Complete_Draft_Into_A_Persisted_Employee()
    {
        var draft = CreateCompleteDraft();
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(new SubmitOnboardingCommand(draft.Id.Value, "EMP-900"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(OnboardingStatus.Converted);
        await _employees.Received(1).AddAsync(
            Arg.Is<Employee>(e => e.Id.Value == result.Value && e.Documents.Count == 1 && e.ConsentRecords.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_And_Not_Persist_An_Employee_When_The_Draft_Is_Incomplete()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(new SubmitOnboardingCommand(draft.Id.Value, "EMP-901"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _employees.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
