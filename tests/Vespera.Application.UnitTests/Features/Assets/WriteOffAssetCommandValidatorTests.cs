using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Assets;

public class WriteOffAssetCommandValidatorTests
{
    private readonly WriteOffAssetCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var command = new WriteOffAssetCommand(Guid.NewGuid(), 1000m, Currency.Inr, string.Empty, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Should_Have_Error_When_Amount_Is_Negative()
    {
        var command = new WriteOffAssetCommand(Guid.NewGuid(), -1m, Currency.Inr, "Lost", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new WriteOffAssetCommand(Guid.NewGuid(), 1000m, Currency.Inr, "Lost in transit", null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
