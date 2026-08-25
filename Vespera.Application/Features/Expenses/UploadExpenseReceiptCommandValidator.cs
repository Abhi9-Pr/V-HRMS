using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class UploadExpenseReceiptCommandValidator : AbstractValidator<UploadExpenseReceiptCommand>
{
    public UploadExpenseReceiptCommandValidator()
    {
        RuleFor(command => command.ClaimId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(256);
    }
}
