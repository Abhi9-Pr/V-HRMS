using FluentValidation.TestHelper;
using Vespera.Application.Features.Auth;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ConfirmTotpEnrollmentCommandValidatorTests
{
    private readonly ConfirmTotpEnrollmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Code_Is_Empty()
    {
        var result = _validator.TestValidate(new ConfirmTotpEnrollmentCommand(string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Not_Six_Digits()
    {
        var result = _validator.TestValidate(new ConfirmTotpEnrollmentCommand("12345"));

        result.ShouldHaveValidationErrorFor(c => c.Code);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Code_Is_Valid()
    {
        var result = _validator.TestValidate(new ConfirmTotpEnrollmentCommand("123456"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
