using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class CelebrationsWidgetProviderTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private CelebrationsWidgetProvider CreateProvider() => new(_sender);

    [Fact]
    public void Should_Expose_Its_Widget_Metadata()
    {
        var provider = CreateProvider();

        provider.WidgetKey.Should().Be("celebrations");
        provider.DefaultVisible.Should().BeTrue();
        provider.DefaultSize.Should().Be(WidgetSize.Small);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_The_Senders_Payload_On_Success()
    {
        IReadOnlyList<CelebrationSummaryDto> celebrations = [];
        _sender.Send(Arg.Any<GetUpcomingCelebrationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(celebrations));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(celebrations);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Propagate_The_Senders_Failure()
    {
        var error = Error.Failure("celebrations.unavailable", "Could not load celebrations.");
        _sender.Send(Arg.Any<GetUpcomingCelebrationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<CelebrationSummaryDto>>(error));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
