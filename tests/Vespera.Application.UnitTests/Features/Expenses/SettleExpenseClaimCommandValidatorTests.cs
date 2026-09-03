using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class SettleExpenseClaimCommandValidatorTests
{
    private readonly SettleExpenseClaimCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ExpenseClaimId_Is_Empty()
    {
        var result = _validator.TestValidate(new SettleExpenseClaimCommand(Guid.Empty, Guid.NewGuid(), null));

        result.ShouldHaveValidationErrorFor(c => c.ExpenseClaimId);
    }

    [Fact]
    public void Should_Have_Error_When_PayrollRunId_Is_Empty()
    {
        var result = _validator.TestValidate(new SettleExpenseClaimCommand(Guid.NewGuid(), Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.PayrollRunId);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new SettleExpenseClaimCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
