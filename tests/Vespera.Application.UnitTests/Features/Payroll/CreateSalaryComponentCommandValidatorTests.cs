using FluentValidation.TestHelper;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Payroll;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class CreateSalaryComponentCommandValidatorTests
{
    private readonly CreateSalaryComponentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateSalaryComponentCommand(string.Empty, nameof(SalaryComponentType.Earning), true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        var command = new CreateSalaryComponentCommand(new string('a', 201), nameof(SalaryComponentType.Earning), true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_ComponentType_Is_Not_A_Known_Value()
    {
        var command = new CreateSalaryComponentCommand("Basic", "NotARealType", true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ComponentType);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateSalaryComponentCommand("Basic", nameof(SalaryComponentType.Earning), true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
