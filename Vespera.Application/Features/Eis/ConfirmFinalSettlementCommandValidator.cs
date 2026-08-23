using FluentValidation;

namespace Vespera.Application.Features.Eis;

public sealed class ConfirmFinalSettlementCommandValidator : AbstractValidator<ConfirmFinalSettlementCommand>
{
    public ConfirmFinalSettlementCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
    }
}
