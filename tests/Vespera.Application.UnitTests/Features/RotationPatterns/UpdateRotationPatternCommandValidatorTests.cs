using FluentValidation.TestHelper;
using Vespera.Application.Features.RotationPatterns;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class UpdateRotationPatternCommandValidatorTests
{
    private readonly UpdateRotationPatternCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new UpdateRotationPatternCommand(Guid.Empty, [new RotationPatternDayRequest(0, null)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UpdateRotationPatternCommand(Guid.NewGuid(), [new RotationPatternDayRequest(0, null)]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
