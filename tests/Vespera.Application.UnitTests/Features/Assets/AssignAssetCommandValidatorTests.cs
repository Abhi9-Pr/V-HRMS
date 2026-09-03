using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class AssignAssetCommandValidatorTests
{
    private readonly AssignAssetCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AssetId_Is_Empty()
    {
        var result = _validator.TestValidate(new AssignAssetCommand(Guid.Empty, Guid.NewGuid(), null));

        result.ShouldHaveValidationErrorFor(c => c.AssetId);
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var result = _validator.TestValidate(new AssignAssetCommand(Guid.NewGuid(), Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new AssignAssetCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
