using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Assets;

public class ConfigureAssetDepreciationCommandValidatorTests
{
    private readonly ConfigureAssetDepreciationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_UsefulLifeMonths_Is_Not_Positive()
    {
        var command = new ConfigureAssetDepreciationCommand(Guid.NewGuid(), DepreciationMethod.StraightLine, 0, 20000m, Currency.Inr, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.UsefulLifeMonths);
    }

    [Fact]
    public void Should_Have_Error_When_SalvageValue_Is_Negative()
    {
        var command = new ConfigureAssetDepreciationCommand(Guid.NewGuid(), DepreciationMethod.StraightLine, 24, -1m, Currency.Inr, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.SalvageValue);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new ConfigureAssetDepreciationCommand(Guid.NewGuid(), DepreciationMethod.DecliningBalance, 24, 20000m, Currency.Inr, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
