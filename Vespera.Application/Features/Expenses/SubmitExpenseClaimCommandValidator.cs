using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class SubmitExpenseClaimCommandValidator : AbstractValidator<SubmitExpenseClaimCommand>
{
    public SubmitExpenseClaimCommandValidator()
    {
        RuleFor(command => command.ClaimId).NotEmpty();
    }
}
