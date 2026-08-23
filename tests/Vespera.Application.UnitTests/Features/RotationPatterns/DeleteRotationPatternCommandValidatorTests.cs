using FluentValidation.TestHelper;
using Vespera.Application.Features.RotationPatterns;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class DeleteRotationPatternCommandValidatorTests
{
    private readonly DeleteRotationPatternCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new DeleteRotationPatternCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        var result = _validator.TestValidate(new DeleteRotationPatternCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
