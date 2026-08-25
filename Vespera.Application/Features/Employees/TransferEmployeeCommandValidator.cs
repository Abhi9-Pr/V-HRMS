using FluentValidation;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class TransferEmployeeCommandValidator : AbstractValidator<TransferEmployeeCommand>
{
    public TransferEmployeeCommandValidator()
    {
        RuleFor(command => command.DepartmentId).NotEmpty();
        RuleFor(command => command.DesignationId).NotEmpty();
        RuleFor(command => command.LocationId).NotEmpty();
        RuleFor(command => command.Reason)
            .NotEmpty()
            .Must(reason => Enum.TryParse<EmploymentChangeReason>(reason, ignoreCase: true, out _))
            .WithMessage("Reason must be one of: Hire, Promotion, Transfer, Demotion, Redesignation.");
    }
}
