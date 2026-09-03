using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var result = _validator.TestValidate(new RegisterUserCommand(string.Empty, "super-secret-1", null, []));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Too_Long()
    {
        var result = _validator.TestValidate(new RegisterUserCommand(new string('a', 255) + "@vespera.test", "super-secret-1", null, []));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        var result = _validator.TestValidate(new RegisterUserCommand("user@vespera.test", string.Empty, null, []));

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Too_Short()
    {
        var result = _validator.TestValidate(new RegisterUserCommand("user@vespera.test", "short", null, []));

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new RegisterUserCommand("user@vespera.test", "super-secret-1", null, []));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
