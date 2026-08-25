using FluentValidation;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class CreateExpensePolicyCommandValidator : AbstractValidator<CreateExpensePolicyCommand>
{
    public CreateExpensePolicyCommandValidator()
    {
        RuleFor(command => command.Category).NotEmpty().MaximumLength(100);
        RuleFor(command => command.MaxAmountPerClaim).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ReceiptRequiredAboveAmount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Currency).IsInEnum();
        RuleFor(command => command.MaxAmountSeverity).IsInEnum();
        RuleFor(command => command.ReceiptRequiredSeverity).IsInEnum();
    }
}
