using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Behaviors;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Behaviors;

public class TransactionBehaviorTests
{
    private sealed record TestCommand : IRequest<Result<int>>;

    [Fact]
    public async Task Handle_Should_Save_Changes_When_Handler_Succeeds()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestCommand, Result<int>>(unitOfWork);
        RequestHandlerDelegate<Result<int>> next = () => Task.FromResult(Result.Success(1));

        var response = await behavior.Handle(new TestCommand(), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Not_Save_Changes_When_Handler_Fails()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestCommand, Result<int>>(unitOfWork);
        RequestHandlerDelegate<Result<int>> next = () =>
            Task.FromResult(Result.Failure<int>(Error.Failure("boom", "boom")));

        var response = await behavior.Handle(new TestCommand(), next, CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_Result_When_SaveChanges_Throws_ConcurrencyConflictException()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new ConcurrencyConflictException("conflict", new InvalidOperationException()));
        var behavior = new TransactionBehavior<TestCommand, Result<int>>(unitOfWork);
        RequestHandlerDelegate<Result<int>> next = () => Task.FromResult(Result.Success(1));

        var response = await behavior.Handle(new TestCommand(), next, CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
