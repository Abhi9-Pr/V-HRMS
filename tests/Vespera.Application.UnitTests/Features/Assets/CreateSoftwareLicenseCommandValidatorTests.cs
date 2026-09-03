using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class CreateSoftwareLicenseCommandValidatorTests
{
    private readonly CreateSoftwareLicenseCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ProductName_Is_Empty()
    {
        var result = _validator.TestValidate(new CreateSoftwareLicenseCommand(string.Empty, 5, null, null));

        result.ShouldHaveValidationErrorFor(c => c.ProductName);
    }

    [Fact]
    public void Should_Have_Error_When_ProductName_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new CreateSoftwareLicenseCommand(new string('a', 257), 5, null, null));

        result.ShouldHaveValidationErrorFor(c => c.ProductName);
    }

    [Fact]
    public void Should_Have_Error_When_SeatCount_Is_Not_Positive()
    {
        var result = _validator.TestValidate(new CreateSoftwareLicenseCommand("Figma", 0, null, null));

        result.ShouldHaveValidationErrorFor(c => c.SeatCount);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new CreateSoftwareLicenseCommand("Figma", 5, null, null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
