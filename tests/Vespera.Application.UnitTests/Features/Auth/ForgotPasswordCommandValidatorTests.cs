using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var result = _validator.TestValidate(new ForgotPasswordCommand(string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Too_Long()
    {
        var result = _validator.TestValidate(new ForgotPasswordCommand(new string('a', 255) + "@vespera.test"));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Email_Is_Valid()
    {
        var result = _validator.TestValidate(new ForgotPasswordCommand("user@vespera.test"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
