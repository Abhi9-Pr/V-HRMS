using FluentValidation;

namespace Vespera.Application.Features.Employees;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.WorkEmail).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Phone).NotEmpty();
        RuleFor(command => command.AnnualCtcCurrency)
            .NotEmpty()
            .When(command => command.AnnualCtcAmount is not null);
        RuleFor(command => command.AnnualCtcAmount)
            .GreaterThanOrEqualTo(0)
            .When(command => command.AnnualCtcAmount is not null);
    }
}
