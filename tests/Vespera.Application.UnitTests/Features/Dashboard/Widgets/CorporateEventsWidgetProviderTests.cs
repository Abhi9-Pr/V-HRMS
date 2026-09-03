using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class CorporateEventsWidgetProviderTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private CorporateEventsWidgetProvider CreateProvider() => new(_sender);

    [Fact]
    public void Should_Expose_Its_Widget_Metadata()
    {
        var provider = CreateProvider();

        provider.WidgetKey.Should().Be("corporateEvents");
        provider.DefaultVisible.Should().BeTrue();
        provider.DefaultSize.Should().Be(WidgetSize.Medium);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_The_Senders_Payload_On_Success()
    {
        IReadOnlyList<CorporateEventSummaryDto> events = [];
        _sender.Send(Arg.Any<GetUpcomingCorporateEventsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(events);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Propagate_The_Senders_Failure()
    {
        var error = Error.Failure("corporate_events.unavailable", "Could not load corporate events.");
        _sender.Send(Arg.Any<GetUpcomingCorporateEventsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<CorporateEventSummaryDto>>(error));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
