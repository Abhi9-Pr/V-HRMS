using FluentValidation;

namespace Vespera.Application.Features.Eis;

public sealed class ExitEmployeeCommandValidator : AbstractValidator<ExitEmployeeCommand>
{
    public ExitEmployeeCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.Reason).IsInEnum();
    }
}
