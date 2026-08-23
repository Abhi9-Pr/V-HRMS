using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class UpdateOnboardingPersonalDetailsCommandHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateOnboardingPersonalDetailsCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateOnboardingPersonalDetailsCommandHandler CreateHandler() =>
        new(_drafts, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Draft_Missing()
    {
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns((OnboardingDraft?)null);

        var result = await CreateHandler().Handle(
            new UpdateOnboardingPersonalDetailsCommand(Guid.NewGuid(), "Ada", "Lovelace", "ada@vespera.test", "+14155552671", new DateOnly(1990, 1, 1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("onboarding_draft.not_found");
    }

    [Fact]
    public async Task Handle_Should_Update_The_Draft_When_Found()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(
            new UpdateOnboardingPersonalDetailsCommand(draft.Id.Value, "Ada", "Lovelace", "ada@vespera.test", "+14155552671", new DateOnly(1990, 1, 1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.FirstName.Should().Be("Ada");
        draft.CurrentStep.Should().Be(OnboardingStep.EmploymentDetails);
    }
}
