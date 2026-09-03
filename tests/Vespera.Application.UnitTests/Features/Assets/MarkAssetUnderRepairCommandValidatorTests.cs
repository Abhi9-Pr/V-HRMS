using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class MarkAssetUnderRepairCommandValidatorTests
{
    private readonly MarkAssetUnderRepairCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AssetId_Is_Empty()
    {
        var result = _validator.TestValidate(new MarkAssetUnderRepairCommand(Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.AssetId);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new MarkAssetUnderRepairCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
