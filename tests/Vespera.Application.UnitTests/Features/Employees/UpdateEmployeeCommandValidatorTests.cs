using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees;

namespace Vespera.Application.UnitTests.Features.Employees;

public class UpdateEmployeeCommandValidatorTests
{
    private readonly UpdateEmployeeCommandValidator _validator = new();

    private static UpdateEmployeeCommand ValidCommand() => new(
        Guid.NewGuid(), "Ada", "Lovelace", "ada@vespera.test", "+14155552671", null, null, null, null);

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { FirstName = "" });
        result.ShouldHaveValidationErrorFor(command => command.FirstName);
    }

    [Fact]
    public void Should_Have_Error_When_AnnualCtcAmount_Is_Set_Without_A_Currency()
    {
        var result = _validator.TestValidate(ValidCommand() with { AnnualCtcAmount = 1200000m });
        result.ShouldHaveValidationErrorFor(command => command.AnnualCtcCurrency);
    }

    [Fact]
    public void Should_Not_Have_Error_When_AnnualCtcAmount_Is_Null()
    {
        var result = _validator.TestValidate(ValidCommand() with { AnnualCtcAmount = null, AnnualCtcCurrency = null });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_AnnualCtcAmount_Is_Negative()
    {
        var result = _validator.TestValidate(ValidCommand() with { AnnualCtcAmount = -1m, AnnualCtcCurrency = "Inr" });
        result.ShouldHaveValidationErrorFor(command => command.AnnualCtcAmount);
    }
}
