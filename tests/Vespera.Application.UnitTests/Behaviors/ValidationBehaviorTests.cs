using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Vespera.Application.Behaviors;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record TestCommand(string Value) : IRequest<Result<int>>;

    [Fact]
    public async Task Handle_Should_Call_Next_When_No_Validators_Registered()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<int>>([]);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(42));
        };

        var response = await behavior.Handle(new TestCommand("x"), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Call_Next_When_All_Validators_Pass()
    {
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestCommand, Result<int>>([validator]);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(42));
        };

        var response = await behavior.Handle(new TestCommand("x"), next, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Short_Circuit_When_A_Validator_Fails()
    {
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Value", "Value is required.")]));

        var behavior = new ValidationBehavior<TestCommand, Result<int>>([validator]);
        var nextCalled = false;
        RequestHandlerDelegate<Result<int>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(42));
        };

        var response = await behavior.Handle(new TestCommand(""), next, CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        response.Error.Type.Should().Be(ErrorType.Validation);
        nextCalled.Should().BeFalse();
    }
}
