using FluentValidation;

namespace Vespera.Application.Features.Shifts;

public sealed class CreateShiftCommandValidator : AbstractValidator<CreateShiftCommand>
{
    public CreateShiftCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.GraceMinutes).GreaterThanOrEqualTo(0);
        RuleFor(command => command.BreakMinutes).GreaterThanOrEqualTo(0);
    }
}
