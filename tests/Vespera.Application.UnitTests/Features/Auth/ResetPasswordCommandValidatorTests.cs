using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand(string.Empty, "reset-token", "new-password-123"));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Have_Error_When_ResetToken_Is_Empty()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("user@vespera.test", string.Empty, "new-password-123"));

        result.ShouldHaveValidationErrorFor(c => c.ResetToken);
    }

    [Fact]
    public void Should_Have_Error_When_NewPassword_Is_Too_Short()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("user@vespera.test", "reset-token", "short"));

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("user@vespera.test", "reset-token", "new-password-123"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
