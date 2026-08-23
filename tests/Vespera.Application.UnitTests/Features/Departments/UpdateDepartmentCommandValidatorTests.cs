using FluentValidation.TestHelper;
using Vespera.Application.Features.Departments;

namespace Vespera.Application.UnitTests.Features.Departments;

public class UpdateDepartmentCommandValidatorTests
{
    private readonly UpdateDepartmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new UpdateDepartmentCommand(Guid.Empty, "Engineering", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new UpdateDepartmentCommand(Guid.NewGuid(), string.Empty, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UpdateDepartmentCommand(Guid.NewGuid(), "Engineering", null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
