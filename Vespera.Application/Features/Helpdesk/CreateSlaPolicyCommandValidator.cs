using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CreateSlaPolicyCommandValidator : AbstractValidator<CreateSlaPolicyCommand>
{
    public CreateSlaPolicyCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(256);
        RuleFor(command => command.ResponseTimeHours).GreaterThan(0);
        RuleFor(command => command.ResolutionTimeHours).GreaterThanOrEqualTo(command => command.ResponseTimeHours);
        RuleFor(command => command.BusinessHoursEnd).Must((command, end) => end > command.BusinessHoursStart)
            .WithMessage("Business hours end must be after the start.");
    }
}
