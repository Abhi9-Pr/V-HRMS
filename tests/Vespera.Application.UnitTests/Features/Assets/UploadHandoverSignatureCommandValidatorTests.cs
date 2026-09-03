using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class UploadHandoverSignatureCommandValidatorTests
{
    private readonly UploadHandoverSignatureCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AssignmentId_Is_Empty()
    {
        var result = _validator.TestValidate(new UploadHandoverSignatureCommand(Guid.Empty, [1, 2, 3], "signature.png", null));

        result.ShouldHaveValidationErrorFor(c => c.AssignmentId);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        var result = _validator.TestValidate(new UploadHandoverSignatureCommand(Guid.NewGuid(), [], "signature.png", null));

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Empty()
    {
        var result = _validator.TestValidate(new UploadHandoverSignatureCommand(Guid.NewGuid(), [1, 2, 3], string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new UploadHandoverSignatureCommand(Guid.NewGuid(), [1, 2, 3], new string('a', 257), null));

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new UploadHandoverSignatureCommand(Guid.NewGuid(), [1, 2, 3], "signature.png", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
