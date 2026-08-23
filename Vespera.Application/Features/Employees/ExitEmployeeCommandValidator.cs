using FluentValidation;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class ExitEmployeeCommandValidator : AbstractValidator<ExitEmployeeCommand>
{
    public ExitEmployeeCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty()
            .Must(reason => Enum.TryParse<EmployeeExitReason>(reason, ignoreCase: true, out _))
            .WithMessage("Reason must be one of: Resignation, Termination, Retirement, EndOfContract.");
    }
}
