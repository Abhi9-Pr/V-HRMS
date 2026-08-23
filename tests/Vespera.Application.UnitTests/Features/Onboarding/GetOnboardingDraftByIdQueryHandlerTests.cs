using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class GetOnboardingDraftByIdQueryHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetOnboardingDraftByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Missing()
    {
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns((OnboardingDraft?)null);
        var handler = new GetOnboardingDraftByIdQueryHandler(_drafts, _tenantContext);

        var result = await handler.Handle(new GetOnboardingDraftByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("onboarding_draft.not_found");
    }

    [Fact]
    public async Task Handle_Should_Map_The_Draft_Including_Documents_And_Consents()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        draft.AddDocument(EmployeeDocumentType.Id, "storage-key-1", DateTimeOffset.UtcNow);
        draft.RecordConsent(ConsentType.DataProcessing, DateTimeOffset.UtcNow);
        _drafts.FirstOrDefaultAsync(Arg.Any<ISpecification<OnboardingDraft>>(), Arg.Any<CancellationToken>()).Returns(draft);
        var handler = new GetOnboardingDraftByIdQueryHandler(_drafts, _tenantContext);

        var result = await handler.Handle(new GetOnboardingDraftByIdQuery(draft.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Documents.Should().ContainSingle();
        result.Value.ConsentRecords.Should().ContainSingle();
    }
}
