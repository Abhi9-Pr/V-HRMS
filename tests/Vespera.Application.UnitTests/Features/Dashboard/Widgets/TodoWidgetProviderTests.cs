using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class TodoWidgetProviderTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private TodoWidgetProvider CreateProvider() => new(_sender);

    [Fact]
    public void Should_Expose_Its_Widget_Metadata()
    {
        var provider = CreateProvider();

        provider.WidgetKey.Should().Be("todos");
        provider.DefaultVisible.Should().BeTrue();
        provider.DefaultSize.Should().Be(WidgetSize.Small);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_The_Senders_Payload_On_Success()
    {
        IReadOnlyList<TodoItemDto> todos = [];
        _sender.Send(Arg.Any<GetMyTodoItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(todos));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(todos);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Propagate_The_Senders_Failure()
    {
        var error = Error.Failure("todos.unavailable", "Could not load todo items.");
        _sender.Send(Arg.Any<GetMyTodoItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<TodoItemDto>>(error));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
