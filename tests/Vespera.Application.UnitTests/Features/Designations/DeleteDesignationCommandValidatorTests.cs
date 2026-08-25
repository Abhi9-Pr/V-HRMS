using FluentValidation.TestHelper;
using Vespera.Application.Features.Designations;

namespace Vespera.Application.UnitTests.Features.Designations;

public class DeleteDesignationCommandValidatorTests
{
    private readonly DeleteDesignationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new DeleteDesignationCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Set()
    {
        var command = new DeleteDesignationCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
