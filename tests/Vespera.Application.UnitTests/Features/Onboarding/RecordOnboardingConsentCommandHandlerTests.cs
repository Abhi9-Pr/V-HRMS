using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class RecordOnboardingConsentCommandHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public RecordOnboardingConsentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_Should_Record_Consent_On_The_Draft()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);
        var handler = new RecordOnboardingConsentCommandHandler(_drafts, _tenantContext, _dateTimeProvider);

        var result = await handler.Handle(new RecordOnboardingConsentCommand(draft.Id.Value, "DataProcessing"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draft.ConsentRecords.Should().ContainSingle(c => c.ConsentType == ConsentType.DataProcessing && c.Granted);
    }
}
