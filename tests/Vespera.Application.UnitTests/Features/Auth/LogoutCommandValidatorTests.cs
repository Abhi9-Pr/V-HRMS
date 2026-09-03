using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RefreshToken_Is_Empty()
    {
        var result = _validator.TestValidate(new LogoutCommand(string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public void Should_Not_Have_Error_When_RefreshToken_Is_Provided()
    {
        var result = _validator.TestValidate(new LogoutCommand("raw-refresh-token"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
