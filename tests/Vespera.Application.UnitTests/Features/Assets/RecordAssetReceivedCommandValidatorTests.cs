using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordAssetReceivedCommandValidatorTests
{
    private readonly RecordAssetReceivedCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RecoveryId_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordAssetReceivedCommand(Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.RecoveryId);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new RecordAssetReceivedCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
