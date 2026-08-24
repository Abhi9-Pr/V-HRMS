using FluentValidation;

namespace Vespera.Application.Features.Payroll;

public sealed class OpenPayrollRunCommandValidator : AbstractValidator<OpenPayrollRunCommand>
{
    public OpenPayrollRunCommandValidator()
    {
        RuleFor(command => command.Month).InclusiveBetween(1, 12);
        RuleFor(command => command.Year).GreaterThanOrEqualTo(2000);
    }
}
