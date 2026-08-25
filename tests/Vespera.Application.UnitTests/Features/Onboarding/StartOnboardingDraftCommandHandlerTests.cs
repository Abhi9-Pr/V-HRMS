using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class StartOnboardingDraftCommandHandlerTests
{
    private readonly IWriteRepository<OnboardingDraft> _drafts = Substitute.For<IWriteRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public StartOnboardingDraftCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_Should_Add_A_New_Draft_And_Return_Its_Id()
    {
        var handler = new StartOnboardingDraftCommandHandler(_drafts, _tenantContext, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(new StartOnboardingDraftCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _drafts.Received(1).AddAsync(Arg.Any<OnboardingDraft>(), Arg.Any<CancellationToken>());
    }
}
