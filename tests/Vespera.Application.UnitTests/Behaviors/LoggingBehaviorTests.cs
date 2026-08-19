using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Vespera.Application.Behaviors;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Behaviors;

public class LoggingBehaviorTests
{
    private sealed record TestCommand : IRequest<Result<int>>;

    [Fact]
    public async Task Handle_Should_Return_Whatever_Next_Returns()
    {
        var behavior = new LoggingBehavior<TestCommand, Result<int>>(NullLogger<LoggingBehavior<TestCommand, Result<int>>>.Instance);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(7));
        };

        var response = await behavior.Handle(new TestCommand(), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be(7);
        nextCalled.Should().BeTrue();
    }
}
