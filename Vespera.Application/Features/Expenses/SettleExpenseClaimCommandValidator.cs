using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class SettleExpenseClaimCommandValidator : AbstractValidator<SettleExpenseClaimCommand>
{
    public SettleExpenseClaimCommandValidator()
    {
        RuleFor(command => command.ExpenseClaimId).NotEmpty();
        RuleFor(command => command.PayrollRunId).NotEmpty();
    }
}
