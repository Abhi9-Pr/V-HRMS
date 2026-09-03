using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordDamageAssessmentCommandValidatorTests
{
    private readonly RecordDamageAssessmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RecoveryId_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordDamageAssessmentCommand(Guid.Empty, "Cracked screen", null));

        result.ShouldHaveValidationErrorFor(c => c.RecoveryId);
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordDamageAssessmentCommand(Guid.NewGuid(), string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.Notes);
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new RecordDamageAssessmentCommand(Guid.NewGuid(), new string('a', 1025), null));

        result.ShouldHaveValidationErrorFor(c => c.Notes);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new RecordDamageAssessmentCommand(Guid.NewGuid(), "Cracked screen", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
