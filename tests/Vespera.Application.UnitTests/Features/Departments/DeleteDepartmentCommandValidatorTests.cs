using FluentValidation.TestHelper;
using Vespera.Application.Features.Departments;

namespace Vespera.Application.UnitTests.Features.Departments;

public class DeleteDepartmentCommandValidatorTests
{
    private readonly DeleteDepartmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new DeleteDepartmentCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Set()
    {
        var command = new DeleteDepartmentCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
