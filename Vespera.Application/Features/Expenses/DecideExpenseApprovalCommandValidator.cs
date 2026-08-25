using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class DecideExpenseApprovalCommandValidator : AbstractValidator<DecideExpenseApprovalCommand>
{
    public DecideExpenseApprovalCommandValidator()
    {
        RuleFor(command => command.ClaimId).NotEmpty();
        RuleFor(command => command.Comment).NotEmpty().When(command => !command.Approved);
    }
}
