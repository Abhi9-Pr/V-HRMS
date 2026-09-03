using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class OpenExpenseClaimCommandValidatorTests
{
    private readonly OpenExpenseClaimCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_SettlementCurrency_Is_Not_A_Defined_Enum_Value()
    {
        var result = _validator.TestValidate(new OpenExpenseClaimCommand((Currency)999, null));

        result.ShouldHaveValidationErrorFor(c => c.SettlementCurrency);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new OpenExpenseClaimCommand(Currency.Inr, null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
