using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class AnnouncementsWidgetProviderTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private AnnouncementsWidgetProvider CreateProvider() => new(_sender);

    [Fact]
    public void Should_Expose_Its_Widget_Metadata()
    {
        var provider = CreateProvider();

        provider.WidgetKey.Should().Be("announcements");
        provider.DefaultVisible.Should().BeTrue();
        provider.DefaultSize.Should().Be(WidgetSize.Medium);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_The_Senders_Payload_On_Success()
    {
        IReadOnlyList<AnnouncementSummaryDto> announcements = [];
        _sender.Send(Arg.Any<GetAnnouncementsForMeQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(announcements));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(announcements);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Propagate_The_Senders_Failure()
    {
        var error = Error.Failure("announcements.unavailable", "Could not load announcements.");
        _sender.Send(Arg.Any<GetAnnouncementsForMeQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<AnnouncementSummaryDto>>(error));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
