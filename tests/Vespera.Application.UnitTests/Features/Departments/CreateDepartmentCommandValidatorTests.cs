using FluentValidation.TestHelper;
using Vespera.Application.Features.Departments;

namespace Vespera.Application.UnitTests.Features.Departments;

public class CreateDepartmentCommandValidatorTests
{
    private readonly CreateDepartmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateDepartmentCommand(string.Empty, "ENG", null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Empty()
    {
        var command = new CreateDepartmentCommand("Engineering", string.Empty, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Code);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateDepartmentCommand("Engineering", "ENG", null, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
