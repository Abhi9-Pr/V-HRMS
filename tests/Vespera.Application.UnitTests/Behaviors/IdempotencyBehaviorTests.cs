using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Behaviors;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Behaviors;

public class IdempotencyBehaviorTests
{
    private sealed record PlainCommand : IRequest<Result<int>>;

    private sealed record IdempotentCommand(string? IdempotencyKey) : IRequest<Result<int>>, IIdempotentRequest;

    [Fact]
    public async Task Handle_Should_Call_Next_When_Request_Is_Not_Idempotent()
    {
        var store = Substitute.For<IIdempotencyStore>();
        var behavior = new IdempotencyBehavior<PlainCommand, Result<int>>(store);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(1));
        };

        var response = await behavior.Handle(new PlainCommand(), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
        await store.DidNotReceiveWithAnyArgs().HasBeenProcessedAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Call_Next_When_Key_Is_Null()
    {
        var store = Substitute.For<IIdempotencyStore>();
        var behavior = new IdempotencyBehavior<IdempotentCommand, Result<int>>(store);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(1));
        };

        var response = await behavior.Handle(new IdempotentCommand(null), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Short_Circuit_When_Key_Already_Processed()
    {
        var store = Substitute.For<IIdempotencyStore>();
        store.HasBeenProcessedAsync("key-1", Arg.Any<CancellationToken>()).Returns(true);

        var behavior = new IdempotencyBehavior<IdempotentCommand, Result<int>>(store);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(1));
        };

        var response = await behavior.Handle(new IdempotentCommand("key-1"), next, CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        response.Error.Type.Should().Be(ErrorType.Conflict);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Mark_Processed_When_New_Key_And_Handler_Succeeds()
    {
        var store = Substitute.For<IIdempotencyStore>();
        store.HasBeenProcessedAsync("key-1", Arg.Any<CancellationToken>()).Returns(false);

        var behavior = new IdempotencyBehavior<IdempotentCommand, Result<int>>(store);
        RequestHandlerDelegate<Result<int>> next = () => Task.FromResult(Result.Success(1));

        var response = await behavior.Handle(new IdempotentCommand("key-1"), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        await store.Received(1).MarkAsProcessedAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Not_Mark_Processed_When_Handler_Fails()
    {
        var store = Substitute.For<IIdempotencyStore>();
        store.HasBeenProcessedAsync("key-1", Arg.Any<CancellationToken>()).Returns(false);

        var behavior = new IdempotencyBehavior<IdempotentCommand, Result<int>>(store);
        RequestHandlerDelegate<Result<int>> next = () =>
            Task.FromResult(Result.Failure<int>(Error.Failure("boom", "boom")));

        var response = await behavior.Handle(new IdempotentCommand("key-1"), next, CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        await store.DidNotReceive().MarkAsProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
