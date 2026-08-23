using FluentValidation;

namespace Vespera.Application.Features.Eis;

public sealed class ConfirmAssetsRecoveredCommandValidator : AbstractValidator<ConfirmAssetsRecoveredCommand>
{
    public ConfirmAssetsRecoveredCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
    }
}
