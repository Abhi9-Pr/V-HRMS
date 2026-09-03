using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var result = _validator.TestValidate(new LoginCommand(string.Empty, "password", "device-1", null));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Too_Long()
    {
        var result = _validator.TestValidate(new LoginCommand(new string('a', 255) + "@vespera.test", "password", "device-1", null));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        var result = _validator.TestValidate(new LoginCommand("user@vespera.test", string.Empty, "device-1", null));

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Empty()
    {
        var result = _validator.TestValidate(new LoginCommand("user@vespera.test", "password", string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.DeviceId);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Too_Long()
    {
        var result = _validator.TestValidate(new LoginCommand("user@vespera.test", "password", new string('d', 257), null));

        result.ShouldHaveValidationErrorFor(c => c.DeviceId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new LoginCommand("user@vespera.test", "password", "device-1", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
