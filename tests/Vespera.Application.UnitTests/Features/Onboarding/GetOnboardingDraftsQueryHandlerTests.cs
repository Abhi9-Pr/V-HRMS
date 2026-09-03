using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class GetOnboardingDraftsQueryHandlerTests
{
    private readonly IReadRepository<OnboardingDraft> _drafts = Substitute.For<IReadRepository<OnboardingDraft>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetOnboardingDraftsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetOnboardingDraftsQueryHandler CreateHandler() => new(_drafts, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_Mapped_Summaries()
    {
        var draft = OnboardingDraft.StartDraft(_tenantId, DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        _drafts.ListAsync(Arg.Any<OnboardingDraftsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([draft]);
        _drafts.CountAsync(Arg.Any<OnboardingDraftsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new GetOnboardingDraftsQuery(new PagedRequest(1, 20)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(draft.Id.Value);
        result.Value.Items[0].Status.Should().Be(draft.Status.ToString());
        result.Value.TotalCount.Should().Be(1);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_Page_When_No_Drafts_Match()
    {
        _drafts.ListAsync(Arg.Any<OnboardingDraftsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
        _drafts.CountAsync(Arg.Any<OnboardingDraftsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateHandler().Handle(new GetOnboardingDraftsQuery(new PagedRequest(1, 20)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
