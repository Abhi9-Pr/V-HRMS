using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class OpenExpenseClaimCommandValidator : AbstractValidator<OpenExpenseClaimCommand>
{
    public OpenExpenseClaimCommandValidator()
    {
        RuleFor(command => command.SettlementCurrency).IsInEnum();
    }
}
