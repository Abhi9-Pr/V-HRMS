using FluentValidation.TestHelper;
using Vespera.Application.Features.Payroll;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class OpenPayrollRunCommandValidatorTests
{
    private readonly OpenPayrollRunCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Should_Have_Error_When_Month_Is_Out_Of_Range(int month)
    {
        var command = new OpenPayrollRunCommand(month, 2026, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Month);
    }

    [Fact]
    public void Should_Have_Error_When_Year_Is_Before_2000()
    {
        var command = new OpenPayrollRunCommand(1, 1999, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Year);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new OpenPayrollRunCommand(5, 2026, "idem-key");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
