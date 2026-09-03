using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CurrentPassword_Is_Empty()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(string.Empty, "new-password-123"));

        result.ShouldHaveValidationErrorFor(c => c.CurrentPassword);
    }

    [Fact]
    public void Should_Have_Error_When_NewPassword_Is_Empty()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand("old-password", string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void Should_Have_Error_When_NewPassword_Is_Too_Short()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand("old-password", "short"));

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand("old-password", "new-password-123"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
