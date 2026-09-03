using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordAssetConditionCommandValidatorTests
{
    private readonly RecordAssetConditionCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AssignmentId_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordAssetConditionCommand(Guid.Empty, AssetConditionRating.Good, null, null));

        result.ShouldHaveValidationErrorFor(c => c.AssignmentId);
    }

    [Fact]
    public void Should_Have_Error_When_Rating_Is_Not_A_Defined_Enum_Value()
    {
        var result = _validator.TestValidate(new RecordAssetConditionCommand(Guid.NewGuid(), (AssetConditionRating)999, null, null));

        result.ShouldHaveValidationErrorFor(c => c.Rating);
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new RecordAssetConditionCommand(Guid.NewGuid(), AssetConditionRating.Good, new string('a', 1025), null));

        result.ShouldHaveValidationErrorFor(c => c.Notes);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new RecordAssetConditionCommand(Guid.NewGuid(), AssetConditionRating.Good, "Looks fine", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
