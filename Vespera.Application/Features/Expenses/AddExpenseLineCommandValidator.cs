using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class AddExpenseLineCommandValidator : AbstractValidator<AddExpenseLineCommand>
{
    public AddExpenseLineCommandValidator()
    {
        RuleFor(command => command.ClaimId).NotEmpty();
        RuleFor(command => command.Category).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.Currency).IsInEnum();
        RuleFor(command => command.TaxAmount).GreaterThanOrEqualTo(0).When(command => command.TaxAmount is not null);
    }
}
