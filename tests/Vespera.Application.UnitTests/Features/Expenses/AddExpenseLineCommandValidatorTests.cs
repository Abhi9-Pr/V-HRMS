using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class AddExpenseLineCommandValidatorTests
{
    private readonly AddExpenseLineCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Category_Is_Empty()
    {
        var command = new AddExpenseLineCommand(Guid.NewGuid(), "", 100m, Currency.Inr, DateOnly.FromDateTime(DateTime.UtcNow), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Should_Have_Error_When_Amount_Is_Not_Positive()
    {
        var command = new AddExpenseLineCommand(Guid.NewGuid(), "Travel", 0m, Currency.Inr, DateOnly.FromDateTime(DateTime.UtcNow), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void Should_Not_Have_Errors_For_A_Valid_Command()
    {
        var command = new AddExpenseLineCommand(
            Guid.NewGuid(), "Travel", 1500m, Currency.Inr, DateOnly.FromDateTime(DateTime.UtcNow), "receipt-1", "Uber", 50m, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
