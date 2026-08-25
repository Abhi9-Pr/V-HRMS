using FluentValidation;

namespace Vespera.Application.Features.Shifts;

public sealed class UpdateShiftCommandValidator : AbstractValidator<UpdateShiftCommand>
{
    public UpdateShiftCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.BreakMinutes).GreaterThanOrEqualTo(0);
    }
}
