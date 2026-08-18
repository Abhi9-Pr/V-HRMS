using FluentAssertions;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_Should_Produce_A_Successful_Result_With_No_Error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_Should_Produce_A_Failed_Result_Carrying_The_Error()
    {
        var error = Error.Conflict("tenant.exists", "Tenant already exists.");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_With_Value_Should_Expose_The_Value()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_With_Value_Should_Throw_When_Value_Is_Accessed()
    {
        var result = Result.Failure<int>(Error.NotFound("x", "not found"));

        var accessingValue = () => result.Value;

        accessingValue.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Implicit_Conversion_From_Value_Should_Produce_A_Successful_Result()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Constructing_A_Successful_Result_With_An_Error_Should_Throw()
    {
        var constructing = () => new Result(true, Error.Conflict("x", "y"));

        constructing.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructing_A_Failed_Result_Without_An_Error_Should_Throw()
    {
        var constructing = () => new Result(false, Error.None);

        constructing.Should().Throw<InvalidOperationException>();
    }
}
