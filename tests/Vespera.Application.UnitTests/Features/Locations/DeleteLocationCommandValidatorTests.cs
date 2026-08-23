using FluentValidation.TestHelper;
using Vespera.Application.Features.Locations;

namespace Vespera.Application.UnitTests.Features.Locations;

public class DeleteLocationCommandValidatorTests
{
    private readonly DeleteLocationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new DeleteLocationCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Set()
    {
        var command = new DeleteLocationCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
