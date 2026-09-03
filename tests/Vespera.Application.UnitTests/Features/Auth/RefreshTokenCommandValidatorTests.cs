using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RefreshToken_Is_Empty()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand(string.Empty, "device-1"));

        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Empty()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand("raw-token", string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.DeviceId);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Too_Long()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand("raw-token", new string('d', 257)));

        result.ShouldHaveValidationErrorFor(c => c.DeviceId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand("raw-token", "device-1"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
