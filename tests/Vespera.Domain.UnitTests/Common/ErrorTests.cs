using FluentAssertions;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Common;

public class ErrorTests
{
    [Fact]
    public void None_Should_Have_Empty_Code_And_Message()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Errors_With_Same_Code_Message_And_Type_Should_Be_Equal()
    {
        var first = Error.NotFound("employee.not_found", "Employee was not found.");
        var second = Error.NotFound("employee.not_found", "Employee was not found.");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Errors_With_Different_Types_Should_Not_Be_Equal()
    {
        var validation = Error.Validation("employee.invalid", "Invalid employee.");
        var conflict = Error.Conflict("employee.invalid", "Invalid employee.");

        validation.Should().NotBe(conflict);
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void Factory_Methods_Should_Set_The_Matching_Type(ErrorType expected)
    {
        var error = expected switch
        {
            ErrorType.Failure => Error.Failure("code", "message"),
            ErrorType.Validation => Error.Validation("code", "message"),
            ErrorType.NotFound => Error.NotFound("code", "message"),
            ErrorType.Conflict => Error.Conflict("code", "message"),
            ErrorType.Unauthorized => Error.Unauthorized("code", "message"),
            ErrorType.Forbidden => Error.Forbidden("code", "message"),
            _ => throw new ArgumentOutOfRangeException(nameof(expected)),
        };

        error.Type.Should().Be(expected);
    }
}
