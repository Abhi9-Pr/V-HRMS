using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Assets;

public class CreateAssetCommandValidatorTests
{
    private readonly CreateAssetCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AssetTag_Is_Empty()
    {
        var command = new CreateAssetCommand(string.Empty, "Laptop", 80000m, Currency.Inr, new DateOnly(2026, 1, 1), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AssetTag);
    }

    [Fact]
    public void Should_Have_Error_When_Category_Is_Empty()
    {
        var command = new CreateAssetCommand("AST-001", string.Empty, 80000m, Currency.Inr, new DateOnly(2026, 1, 1), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Should_Have_Error_When_PurchaseCost_Is_Negative()
    {
        var command = new CreateAssetCommand("AST-001", "Laptop", -1m, Currency.Inr, new DateOnly(2026, 1, 1), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.PurchaseCost);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateAssetCommand("AST-001", "Laptop", 80000m, Currency.Inr, new DateOnly(2026, 1, 1), "SN-1", "AA:BB", null, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
