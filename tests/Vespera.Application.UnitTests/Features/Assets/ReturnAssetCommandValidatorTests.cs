using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class ReturnAssetCommandValidatorTests
{
    private readonly ReturnAssetCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AssignmentId_Is_Empty()
    {
        var result = _validator.TestValidate(new ReturnAssetCommand(Guid.Empty, "Good condition", null));

        result.ShouldHaveValidationErrorFor(c => c.AssignmentId);
    }

    [Fact]
    public void Should_Have_Error_When_Condition_Is_Empty()
    {
        var result = _validator.TestValidate(new ReturnAssetCommand(Guid.NewGuid(), string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.Condition);
    }

    [Fact]
    public void Should_Have_Error_When_Condition_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new ReturnAssetCommand(Guid.NewGuid(), new string('a', 1025), null));

        result.ShouldHaveValidationErrorFor(c => c.Condition);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new ReturnAssetCommand(Guid.NewGuid(), "Good condition", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
