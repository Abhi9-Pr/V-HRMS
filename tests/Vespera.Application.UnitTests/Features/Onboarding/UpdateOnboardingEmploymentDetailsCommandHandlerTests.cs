using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class UpdateOnboardingEmploymentDetailsCommandHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateOnboardingEmploymentDetailsCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_Should_Update_Employment_Details_And_Advance_Step()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);
        var handler = new UpdateOnboardingEmploymentDetailsCommandHandler(_drafts, _tenantContext, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(
            new UpdateOnboardingEmploymentDetailsCommand(draft.Id.Value, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 2, 1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.DateOfJoining.Should().Be(new DateOnly(2026, 2, 1));
        draft.CurrentStep.Should().Be(OnboardingStep.Documents);
    }
}
